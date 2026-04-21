namespace DiffingApi.UnitTests;

public class DiffContentStoreTests
{
    [Fact]
    public void SetLeft_WhenEntryDoesNotExist_CreatesEntryWithLeftPayload()
    {
        var store = new InMemoryDiffContentStore();
        var left = new byte[] { 1, 2, 3 };

        store.SetLeft("id", left);

        var found = store.TryGet("id", out var entry);

        Assert.True(found);
        Assert.NotNull(entry);
        Assert.Same(left, entry!.Left);
        Assert.Null(entry.Right);
    }

    [Fact]
    public void SetRight_WhenLeftAlreadyExists_PreservesLeftPayload()
    {
        var store = new InMemoryDiffContentStore();
        var left = new byte[] { 1, 2, 3 };
        var right = new byte[] { 4, 5, 6 };

        store.SetLeft("id", left);
        store.SetRight("id", right);

        var found = store.TryGet("id", out var entry);

        Assert.True(found);
        Assert.NotNull(entry);
        Assert.Same(left, entry!.Left);
        Assert.Same(right, entry.Right);
    }

    [Fact]
    public void SetRight_WhenExistingEntryIsRead_DoesNotMutatePreviouslyReadSnapshot()
    {
        var store = new InMemoryDiffContentStore();

        store.SetLeft("id", new byte[] { 1, 2, 3 });
        store.TryGet("id", out var snapshot);

        store.SetRight("id", new byte[] { 4, 5, 6 });
        store.TryGet("id", out var updatedEntry);

        Assert.NotNull(snapshot);
        Assert.Null(snapshot!.Right);
        Assert.NotNull(updatedEntry);
        Assert.NotSame(snapshot, updatedEntry);
        Assert.NotNull(updatedEntry!.Right);
    }
}
