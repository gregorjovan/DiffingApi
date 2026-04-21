namespace DiffingApi.Advanced.Infrastructure.Persistence;

public sealed class DiffEntryEntity
{
    public required string Id { get; init; }
    public byte[]? Left { get; set; }
    public byte[]? Right { get; set; }
}
