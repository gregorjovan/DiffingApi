namespace DiffingApi.Advanced.Application.Abstractions
{
    public interface IDiffService
    {
        Task SaveLeftAsync(string id, byte[] data, CancellationToken ct = default);
        Task SaveRightAsync(string id, byte[] data, CancellationToken ct = default);
        Task<DiffResult?> GetDiffAsync(string id, CancellationToken ct = default);
        Task<DiffStatusResult?> GetDiffStatusAsync(string id, CancellationToken ct = default);
    }
}
