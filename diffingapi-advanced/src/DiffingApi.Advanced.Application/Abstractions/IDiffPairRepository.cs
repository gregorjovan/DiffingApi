namespace DiffingApi.Advanced.Application.Abstractions;

public interface IDiffPairRepository
{
    Task<DiffPairEntity?> GetAsync(string id, CancellationToken ct = default);
    Task UpsertLeftAsync(string id, byte[] data, CancellationToken ct = default);
    Task UpsertRightAsync(string id, byte[] data, CancellationToken ct = default);
    Task MarkPendingAsync(string id, CancellationToken ct = default);
    Task MarkProcessingAsync(string id, CancellationToken ct = default);
    Task SaveDiffResultAsync(string id, DiffResult result, CancellationToken ct = default);
    Task SaveDiffFailureAsync(string id, string reason, CancellationToken ct = default);
}
