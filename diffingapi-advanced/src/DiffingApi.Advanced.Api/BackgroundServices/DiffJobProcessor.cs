using DiffingApi.Advanced.Application.Abstractions;
using DiffingApi.Advanced.Application.Services;
using Microsoft.Extensions.Options;

namespace DiffingApi.Advanced.Api.BackgroundServices;

public sealed class DiffJobProcessor : BackgroundService
{
    private readonly IDiffJobQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DiffJobProcessor> _logger;
    private readonly SemaphoreSlim _concurrencyLimiter;

    public DiffJobProcessor(
        IDiffJobQueue queue,
        IServiceScopeFactory scopeFactory,
        IOptions<DiffJobOptions> options,
        ILogger<DiffJobProcessor> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _concurrencyLimiter = new SemaphoreSlim(Math.Max(1, options.Value.MaxConcurrency));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            string id;

            try
            {
                id = await _queue.DequeueAsync(stoppingToken);
                await _concurrencyLimiter.WaitAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            _ = ProcessJobAndReleaseAsync(id, stoppingToken);
        }
    }

    private async Task ProcessJobAndReleaseAsync(string id, CancellationToken ct)
    {
        try
        {
            await ProcessJobAsync(id, ct);
        }
        finally
        {
            _concurrencyLimiter.Release();
        }
    }

    private async Task ProcessJobAsync(string id, CancellationToken ct)
    {
        if (!_queue.TryStart(id, out var generation))
        {
            return;
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IDiffPairRepository>();
            var lockProvider = scope.ServiceProvider.GetRequiredService<IDiffLockProvider>();

            await using (await lockProvider.AcquireAsync(id, ct))
            {
                await repository.MarkProcessingAsync(id, ct);

                var pair = await repository.GetAsync(id, ct);

                if (pair?.Left is null || pair.Right is null)
                {
                    await repository.SaveDiffFailureAsync(
                        id,
                        "Both left and right payloads are required.",
                        ct);
                    await _queue.FailAsync(id, generation, ct);
                    return;
                }

                var result = DiffResultFactory.Create(pair.Left, pair.Right);
                await repository.SaveDiffResultAsync(id, result, ct);
                await _queue.CompleteAsync(id, generation, ct);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Diff job failed for id {Id}", id);
            using var scope = _scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IDiffPairRepository>();

            await repository.SaveDiffFailureAsync(id, ex.Message, CancellationToken.None);
            await _queue.FailAsync(id, generation, CancellationToken.None);
        }
    }
}
