namespace DiffingApi.Advanced.Application.Abstractions;

public interface IDiffLockProvider
{
    ValueTask<IAsyncDisposable> AcquireAsync(string id, CancellationToken ct = default);
}
