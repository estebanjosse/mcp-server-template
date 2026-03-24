[CmdletBinding()]
param(
    [string]$OutputPath = (Join-Path $PSScriptRoot "..\src\McpServer.Template.Host.Http\certs\mcp-server-dev.pfx"),
    [string]$Password = "changeit",
    [switch]$Trust
)

$ErrorActionPreference = "Stop"

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue))
{
    throw "The dotnet CLI is required to generate a development certificate."
}

if ([string]::IsNullOrWhiteSpace($Password) -or $Password.Length -lt 8)
{
    throw "Provide a certificate password with at least 8 characters."
}

if ([System.IO.Path]::IsPathRooted($OutputPath))
{
    $resolvedOutputPath = [System.IO.Path]::GetFullPath($OutputPath)
}
else
{
    $resolvedOutputPath = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot $OutputPath))
}

$outputDirectory = Split-Path -Parent $resolvedOutputPath
New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null

$arguments = @(
    "dev-certs",
    "https",
    "--export-path", $resolvedOutputPath,
    "--password", $Password
)

if ($Trust)
{
    $arguments += "--trust"
}

Write-Host "Exporting development certificate to $resolvedOutputPath"
& dotnet @arguments

if ($LASTEXITCODE -ne 0)
{
    throw "dotnet dev-certs failed with exit code $LASTEXITCODE."
}

Write-Host ""
Write-Host "Certificate exported successfully."
Write-Host "Use this Kestrel configuration in src/McpServer.Template.Host.Http/appsettings.Development.json:"
Write-Host @"
{
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://localhost:5000"
      },
      "Https": {
        "Url": "https://localhost:5001",
        "Certificate": {
          "Path": "certs/mcp-server-dev.pfx",
          "Password": "$Password"
        }
      }
    }
  }
}
"@
Write-Host ""
Write-Host "Then run: dotnet run --project src/McpServer.Template.Host.Http"