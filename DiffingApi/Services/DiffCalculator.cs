namespace DiffingApi.Services;

public static class DiffCalculator
{
    public static List<object> FindDiffs(byte[] leftBytes, byte[] rightBytes)
    {
        var diffs = new List<object>();
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

            diffs.Add(new
            {
                offset = currentOffset.Value,
                length = index - currentOffset.Value
            });

            currentOffset = null;
        }

        if (currentOffset is not null)
        {
            diffs.Add(new
            {
                offset = currentOffset.Value,
                length = leftBytes.Length - currentOffset.Value
            });
        }

        return diffs;
    }
}
