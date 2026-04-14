namespace DiffingApi.Services;

public sealed record DiffRange(int Offset, int Length);

public static class DiffCalculator
{
    public static List<DiffRange> FindDiffs(ReadOnlySpan<byte> leftBytes, ReadOnlySpan<byte> rightBytes)
    {
        var diffs = new List<DiffRange>();
        int? currentOffset = null;

        for (var index = 0; index < leftBytes.Length; index++)
        {
            if (leftBytes[index] != rightBytes[index])
            {
                currentOffset ??= index;
                continue;
            }

            if (currentOffset is null)
            {
                continue;
            }

            diffs.Add(new DiffRange(
                currentOffset.Value,
                index - currentOffset.Value));

            currentOffset = null;
        }

        if (currentOffset is not null)
        {
            diffs.Add(new DiffRange(
                currentOffset.Value,
                leftBytes.Length - currentOffset.Value));
        }

        return diffs;
    }
}