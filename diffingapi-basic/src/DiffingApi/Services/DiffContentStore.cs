using System.Collections.Concurrent;
using DiffingApi.Models;
using DiffingApi.Services;

namespace DiffingApi.Basic.Services;

/// <summary>
/// In-memory storage for uploaded diff payloads.
/// </summary>
public sealed class DiffContentStore
{
    private readonly ConcurrentDictionary<string, DiffEntry> _entries = new();

    /// <summary>
    /// Stores the decoded left payload for the specified identifier.
    /// </summary>
    public void SetLeft(string id, byte[] data)
    {
        _entries.AddOrUpdate(
            id,
            _ => new DiffEntry(Left: data),
            (_, existing) => existing with { Left = data });
    }

    /// <summary>
    /// Stores the decoded right payload for the specified identifier.
    /// </summary>
    public void SetRight(string id, byte[] data)
    {
        _entries.AddOrUpdate(
            id,
            _ => new DiffEntry(Right: data),
            (_, existing) => existing with { Right = data });
    }

    /// <summary>
    /// Tries to retrieve the stored payloads for the specified identifier.
    /// </summary>
    public bool TryGet(string id, out DiffEntry? entry)
    {
        return _entries.TryGetValue(id, out entry);
    }

    /// <summary>
    /// Returns the diff result for the specified identifier when both payloads are available.
    /// </summary>
    public DiffResult? GetDiffResult(string id)
    {
        if (!TryGet(id, out var entry) || entry?.Left is null || entry.Right is null)
        {
            return null;
        }

        var leftBytes = entry.Left;
        var rightBytes = entry.Right;

        if (leftBytes.Length != rightBytes.Length)
        {
            return new DiffResult("SizeDoNotMatch");
        }

        if (leftBytes.SequenceEqual(rightBytes))
        {
            return new DiffResult("Equals");
        }

        var diffs = DiffCalculator.FindDiffs(leftBytes, rightBytes)
            .Select(diff => new DiffRangeResult(diff.Offset, diff.Length))
            .ToArray();

        return new DiffResult("ContentDoNotMatch", diffs);
    }
}
