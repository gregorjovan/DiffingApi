namespace DiffingApi.Models;

/// <summary>
/// Stores the decoded left and right payloads for a single diff identifier.
/// </summary>
public sealed class DiffEntry
{
    public byte[]? Left { get; set; }
    public byte[]? Right { get; set; }
}
