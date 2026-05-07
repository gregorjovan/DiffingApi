using DiffingApi.Advanced.Application.Abstractions;

namespace DiffingApi.Advanced.Application.Services;

public static class DiffResultFactory
{
    public static DiffResult Create(byte[] left, byte[] right)
    {
        if (left.Length != right.Length)
        {
            return new DiffResult("SizeDoNotMatch");
        }

        if (left.SequenceEqual(right))
        {
            return new DiffResult("Equals");
        }

        var diffs = DiffCalculator.FindDiffs(left, right)
            .Select(diff => new DiffRangeResult(diff.Offset, diff.Length))
            .ToArray();

        return new DiffResult("ContentDoNotMatch", diffs);
    }
}
