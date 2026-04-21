namespace DiffingApi.UnitTests;

public sealed class DiffLockProviderTests
{
    [Fact]
    public async Task AcquireAsync_WhenSameIdIsLocked_WaitsUntilLeaseIsReleased()
    {
        var provider = new DiffLockProvider();

        await using var firstLease = await provider.AcquireAsync("id");

        var secondAcquire = provider.AcquireAsync("id").AsTask();

        await Task.Delay(100);

        Assert.False(secondAcquire.IsCompleted);

        await firstLease.DisposeAsync();

        await using var secondLease = await secondAcquire;
    }

    [Fact]
    public async Task AcquireAsync_WhenDifferentIdsAreUsed_DoesNotBlock()
    {
        var provider = new DiffLockProvider();

        await using var firstLease = await provider.AcquireAsync("left");
        await using var secondLease = await provider.AcquireAsync("right");
    }
}
