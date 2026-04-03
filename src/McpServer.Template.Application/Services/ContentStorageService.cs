using McpServer.Template.Application.Ports;
using McpServer.Template.Contracts.DTOs;

namespace McpServer.Template.Application.Services;

public sealed class ContentStorageService : IContentStorageService
{
    public Task<SaveTextResponse> SaveTextAsync(SaveTextRequest request, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Implemented in a later OpenSpec task.");
    }

    public Task<ReadTextResponse> ReadTextAsync(ReadTextRequest request, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Implemented in a later OpenSpec task.");
    }

    public Task<SearchTextResponse> SearchTextAsync(SearchTextRequest request, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Implemented in a later OpenSpec task.");
    }
}