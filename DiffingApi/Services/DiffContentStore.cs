using System.Collections.Concurrent;
using DiffingApi.Models;

namespace DiffingApi.Services;

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
            _ => new DiffEntry { Left = data },
            (_, existing) =>
            {
                existing.Left = data;
                return existing;
            });
    }

    /// <summary>
    /// Stores the decoded right payload for the specified identifier.
    /// </summary>
    public void SetRight(string id, byte[] data)
    {
        _entries.AddOrUpdate(
            id,
            _ => new DiffEntry { Right = data },
            (_, existing) =>
            {
                existing.Right = data;
                return existing;
            });
    }

    /// <summary>
    /// Tries to retrieve the stored payloads for the specified identifier.
    /// </summary>
    public bool TryGet(string id, out DiffEntry? entry)
    {
        return _entries.TryGetValue(id, out entry);
    }
}
