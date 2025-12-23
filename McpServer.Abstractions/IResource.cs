using System.Threading;
using System.Threading.Tasks;

namespace McpServer.Abstractions;

/// <summary>
/// Represents a resource that can be accessed by the MCP client.
/// </summary>
public interface IResource
{
    /// <summary>
    /// Gets the unique URI of the resource.
    /// </summary>
    string Uri { get; }

    /// <summary>
    /// Gets the name of the resource.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the description of the resource.
    /// </summary>
    string? Description { get; }

    /// <summary>
    /// Gets the MIME type of the resource.
    /// </summary>
    string MimeType { get; }

    /// <summary>
    /// Reads the content of the resource.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The content of the resource.</returns>
    Task<string> ReadAsync(CancellationToken cancellationToken = default);
}
