namespace DiffingApi.UnitTests;

public class DiffCalculatorTests
{
    [Fact]
    public void FindDiffs_WhenArraysAreEqual_ReturnsEmptyList()
    {
        var left = new byte[] { 1, 2, 3 };
        var right = new byte[] { 1, 2, 3 };

        var diffs = DiffCalculator.FindDiffs(left, right);

        Assert.Empty(diffs);
    }

    [Fact]
    public void FindDiffs_WhenOneByteDiffers_ReturnsSingleDiff()
    {
        var left = new byte[] { 1, 2, 3 };
        var right = new byte[] { 1, 9, 3 };

        var diffs = DiffCalculator.FindDiffs(left, right);

        Assert.Single(diffs);
    }

    [Fact]
    public void FindDiffs_WhenContiguousBlockDiffers_ReturnsSingleRange()
    {
        var left = new byte[] { 1, 2, 3, 4, 5 };
        var right = new byte[] { 1, 9, 9, 9, 5 };

        var diffs = DiffCalculator.FindDiffs(left, right);

        Assert.Single(diffs);
    }

    [Fact]
    public void FindDiffs_WhenMultipleBlocksDiffer_ReturnsMultipleRanges()
    {
        var left = new byte[] { 1, 2, 3, 4, 5 };
        var right = new byte[] { 1, 9, 3, 8, 5 };

        var diffs = DiffCalculator.FindDiffs(left, right);

        Assert.Equal(2, diffs.Count);
    }
}