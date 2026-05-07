using System.Text.Json.Serialization;

namespace DiffingApi.Advanced.Application.Abstractions;

public sealed record DiffStatusResult(
    string Status,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? DiffResultType = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    IReadOnlyList<DiffRangeResult>? Diffs = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? Reason = null);
