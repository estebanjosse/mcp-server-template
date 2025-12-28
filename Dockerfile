# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files for dependency restoration
COPY ["McpServer.Template.sln", "./"]
COPY ["Directory.Build.props", "./"]
COPY ["Directory.Build.targets", "./"]
COPY ["src/McpServer.Template.Contracts/McpServer.Template.Contracts.csproj", "src/McpServer.Template.Contracts/"]
COPY ["src/McpServer.Template.Infrastructure/McpServer.Template.Infrastructure.csproj", "src/McpServer.Template.Infrastructure/"]
COPY ["src/McpServer.Template.Application/McpServer.Template.Application.csproj", "src/McpServer.Template.Application/"]
COPY ["src/McpServer.Template.Mcp/McpServer.Template.Mcp.csproj", "src/McpServer.Template.Mcp/"]
COPY ["src/McpServer.Template.Host.Http/McpServer.Template.Host.Http.csproj", "src/McpServer.Template.Host.Http/"]

# Restore dependencies (this layer will be cached)
RUN dotnet restore "src/McpServer.Template.Host.Http/McpServer.Template.Host.Http.csproj"

# Copy remaining source code
COPY ["src/", "src/"]

# Build the application
RUN dotnet build "src/McpServer.Template.Host.Http/McpServer.Template.Host.Http.csproj" \
    --configuration Release \
    --no-restore

# Publish the application
RUN dotnet publish "src/McpServer.Template.Host.Http/McpServer.Template.Host.Http.csproj" \
    --configuration Release \
    --no-build \
    --output /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Create non-root user for security
RUN groupadd -r mcpserver && useradd -r -g mcpserver mcpserver

# Copy published application from build stage
COPY --from=build /app/publish .

# Change ownership to non-root user
RUN chown -R mcpserver:mcpserver /app

# Switch to non-root user
USER mcpserver

# Expose HTTP port
EXPOSE 5000

# Health check configuration
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:5000/health || exit 1

# Set entrypoint to bind to all interfaces
ENTRYPOINT ["dotnet", "McpServer.Template.Host.Http.dll", "--urls", "http://+:5000"]
