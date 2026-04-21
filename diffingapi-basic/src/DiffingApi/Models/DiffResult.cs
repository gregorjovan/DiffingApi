using System.Text.Json.Serialization;

namespace DiffingApi.Models;

public sealed record DiffResult(
    string DiffResultType,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    IReadOnlyList<DiffRangeResult>? Diffs = null);

public sealed record DiffRangeResult(
    int Offset,
    int Length);
