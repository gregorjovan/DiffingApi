namespace DiffingApi.Advanced.Domain.Models;

/// <summary>
/// Stores the decoded left and right payloads for a single diff identifier.
/// </summary>
public sealed record DiffEntry(byte[]? Left = null, byte[]? Right = null);
