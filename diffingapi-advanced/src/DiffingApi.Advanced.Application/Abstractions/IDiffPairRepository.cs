namespace DiffingApi.Advanced.Application.Abstractions;

public interface IDiffPairRepository
{
    Task<DiffPairEntity?> GetAsync(string id, CancellationToken ct = default);
    Task UpsertLeftAsync(string id, byte[] data, CancellationToken ct = default);
    Task UpsertRightAsync(string id, byte[] data, CancellationToken ct = default);
}