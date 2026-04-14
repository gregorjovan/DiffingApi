using System.Collections.Concurrent;
using System.Linq;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<DiffContentStore>();

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.MapPut("/v1/diff/{id}/left", (string id, DiffRequest request, DiffContentStore store) =>
{
    store.SetLeft(id, request.Data);

    return Results.Created($"/v1/diff/{id}/left", null);
});

app.MapPut("/v1/diff/{id}/right", (string id, DiffRequest request, DiffContentStore store) =>
{
    store.SetRight(id, request.Data);

    return Results.Created($"/v1/diff/{id}/right", null);
});

app.MapGet("/v1/diff/{id}", (string id, DiffContentStore store) =>
{
    if (!store.TryGet(id, out var entry) || entry is null || entry.Left is null || entry.Right is null)
    {
        return Results.NotFound();
    }

    var leftData = entry.Left;
    var rightData = entry.Right;

    var leftBytes = Convert.FromBase64String(leftData);
    var rightBytes = Convert.FromBase64String(rightData);

    if (leftBytes.SequenceEqual(rightBytes))
    {
        return Results.Ok(new { diffResultType = "Equals" });
    }

    if (leftBytes.Length != rightBytes.Length)
    {
        return Results.Ok(new { diffResultType = "SizeDoNotMatch" });
    }

    var diffs = DiffCalculator.FindDiffs(leftBytes, rightBytes);

    return Results.Ok(new { diffResultType = "ContentDoNotMatch", diffs });
});

app.Run();

public partial class Program;

public sealed record DiffRequest(string Data);

public sealed class DiffContentStore
{
    private readonly ConcurrentDictionary<string, DiffEntry> _entries = new();

    public void SetLeft(string id, string data)
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

    public void SetRight(string id, string data)
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

public sealed class DiffEntry
{
    public string? Left { get; set; }
    public string? Right { get; set; }
}

public static class DiffCalculator
{
    public static List<object> FindDiffs(byte[] leftBytes, byte[] rightBytes)
    {
        var diffs = new List<object>();
        int? currentOffset = null;

        for (var index = 0; index < leftBytes.Length; index++)
        {
            if (leftBytes[index] != rightBytes[index])
            {
                currentOffset ??= index;
                continue;
            }

            if (currentOffset is null)
            {
                continue;
            }

            diffs.Add(new
            {
                offset = currentOffset.Value,
                length = index - currentOffset.Value
            });

            currentOffset = null;
        }

        if (currentOffset is not null)
        {
            diffs.Add(new
            {
                offset = currentOffset.Value,
                length = leftBytes.Length - currentOffset.Value
            });
        }

        return diffs;
    }
}
