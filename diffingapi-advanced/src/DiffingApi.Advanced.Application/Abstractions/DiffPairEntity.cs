namespace DiffingApi.Advanced.Application.Abstractions
{
    public sealed class DiffPairEntity
    {
        public string Id { get; set; } = default!;
        public byte[]? Left { get; set; }
        public byte[]? Right { get; set; }
    }
}
