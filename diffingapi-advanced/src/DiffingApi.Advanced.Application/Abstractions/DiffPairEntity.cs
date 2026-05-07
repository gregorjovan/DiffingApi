namespace DiffingApi.Advanced.Application.Abstractions
{
    public sealed class DiffPairEntity
    {
        public string Id { get; set; } = default!;
        public byte[]? Left { get; set; }
        public byte[]? Right { get; set; }
        public string? ProcessingStatus { get; set; }
        public string? DiffResultType { get; set; }
        public string? DiffsJson { get; set; }
        public string? FailureReason { get; set; }
    }
}
