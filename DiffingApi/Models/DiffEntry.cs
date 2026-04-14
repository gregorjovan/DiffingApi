namespace DiffingApi.Models;

public sealed class DiffEntry
{
    public byte[]? Left { get; set; }
    public byte[]? Right { get; set; }
}
