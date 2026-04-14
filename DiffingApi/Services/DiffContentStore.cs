using System.Collections.Concurrent;
using DiffingApi.Models;

namespace DiffingApi.Services;

public sealed class DiffContentStore
{
    private readonly ConcurrentDictionary<string, DiffEntry> _entries = new();

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

    public bool TryGet(string id, out DiffEntry? entry)
    {
        return _entries.TryGetValue(id, out entry);
    }
}
