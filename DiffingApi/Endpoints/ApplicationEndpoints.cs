using DiffingApi.Contracts;
using DiffingApi.Services;

namespace DiffingApi.Endpoints;

public static class ApplicationEndpoints
{
    public static void MapApplicationEndpoints(this WebApplication app)
    {
        var diffGroup = app.MapGroup("/v1/diff");

        app.MapPut("/{id}/left", (string id, DiffRequest request, DiffContentStore store) =>
        {
            store.SetLeft(id, request.Data);
            return Results.Created($"/v1/diff/{id}/left", null);
        });

        app.MapPut("/{id}/right", (string id, DiffRequest request, DiffContentStore store) =>
        {
            store.SetRight(id, request.Data);
            return Results.Created($"/v1/diff/{id}/right", null);
        });

        app.MapGet("/{id}", (string id, DiffContentStore store) =>
        {
            if (!store.TryGet(id, out var entry) || entry?.Left is null || entry.Right is null)
                return Results.NotFound();

            var leftBytes = Convert.FromBase64String(entry.Left);
            var rightBytes = Convert.FromBase64String(entry.Right);

            if (leftBytes.SequenceEqual(rightBytes))
                return Results.Ok(new { diffResultType = "Equals" });

            if (leftBytes.Length != rightBytes.Length)
                return Results.Ok(new { diffResultType = "SizeDoNotMatch" });

            var diffs = DiffCalculator.FindDiffs(leftBytes, rightBytes);

            return Results.Ok(new { diffResultType = "ContentDoNotMatch", diffs });
        });
    }
}