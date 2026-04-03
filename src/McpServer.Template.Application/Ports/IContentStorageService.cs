using McpServer.Template.Contracts.DTOs;

namespace McpServer.Template.Application.Ports;

public interface IContentStorageService
{
    Task<SaveTextResponse> SaveTextAsync(SaveTextRequest request, CancellationToken cancellationToken = default);

    Task<ReadTextResponse> ReadTextAsync(ReadTextRequest request, CancellationToken cancellationToken = default);

    Task<SearchTextResponse> SearchTextAsync(SearchTextRequest request, CancellationToken cancellationToken = default);
}