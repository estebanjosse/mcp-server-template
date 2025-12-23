using System.Threading;
using System.Threading.Tasks;

namespace McpServer.Abstractions;

/// <summary>
/// Represents an MCP server that can handle client requests.
/// </summary>
public interface IMcpServer
{
    /// <summary>
    /// Starts the MCP server and begins accepting client connections.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RunAsync(CancellationToken cancellationToken = default);
}
