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
        Assert.Equal(new DiffRange(1, 1), diffs[0]);
    }

    [Fact]
    public void FindDiffs_WhenContiguousBlockDiffers_ReturnsSingleRange()
    {
        var left = new byte[] { 1, 2, 3, 4, 5 };
        var right = new byte[] { 1, 9, 9, 9, 5 };

        var diffs = DiffCalculator.FindDiffs(left, right);

        Assert.Single(diffs);
        Assert.Equal(new DiffRange(1, 3), diffs[0]);
    }

    [Fact]
    public void FindDiffs_WhenMultipleBlocksDiffer_ReturnsMultipleRanges()
    {
        var left = new byte[] { 1, 2, 3, 4, 5 };
        var right = new byte[] { 1, 9, 3, 8, 5 };

        var diffs = DiffCalculator.FindDiffs(left, right);

        Assert.Equal(2, diffs.Count);
        Assert.Equal(new DiffRange(1, 1), diffs[0]);
        Assert.Equal(new DiffRange(3, 1), diffs[1]);
    }

    [Fact]
    public void FindDiffs_WhenDifferenceIsAtStart_ReturnsRangeStartingAtZero()
    {
        var left = new byte[] { 1, 2, 3 };
        var right = new byte[] { 9, 2, 3 };

        var diffs = DiffCalculator.FindDiffs(left, right);

        Assert.Single(diffs);
        Assert.Equal(new DiffRange(0, 1), diffs[0]);
    }

    [Fact]
    public void FindDiffs_WhenDifferenceIsAtEnd_ReturnsTrailingRange()
    {
        var left = new byte[] { 1, 2, 3 };
        var right = new byte[] { 1, 2, 9 };

        var diffs = DiffCalculator.FindDiffs(left, right);

        Assert.Single(diffs);
        Assert.Equal(new DiffRange(2, 1), diffs[0]);
    }

    [Fact]
    public void FindDiffs_WhenEntirePayloadDiffers_ReturnsSingleFullLengthRange()
    {
        var left = new byte[] { 1, 2, 3 };
        var right = new byte[] { 9, 9, 9 };

        var diffs = DiffCalculator.FindDiffs(left, right);

        Assert.Single(diffs);
        Assert.Equal(new DiffRange(0, 3), diffs[0]);
    }
}
