using DiffingApi.Advanced.Domain.Models;

namespace DiffingApi.Advanced.Application.Abstractions;

public interface IDiffContentStore
{
    void SetLeft(string id, byte[] data);
    void SetRight(string id, byte[] data);
    bool TryGet(string id, out DiffEntry? entry);
}
