using System.Linq;
using DiffingApi.Contracts;
using DiffingApi.Services;

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
