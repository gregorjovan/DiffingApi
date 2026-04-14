using DiffingApi.Contracts;
using DiffingApi.Services;

namespace DiffingApi.Endpoints;

public static class ApplicationEndpoints
{
    public static void MapApplicationEndpoints(this WebApplication app)
    {
        var diffGroup = app.MapGroup("/v1/diff")
            .WithTags("DiffingApi"); ;

        app.MapPut("/{id}/left", (string id, DiffRequest request, DiffContentStore store) =>
        {
            store.SetLeft(id, request.Data);
            return Results.Created($"/v1/diff/{id}/left", null);
        })
            .WithName("PutLeft")
            .WithTags("PutLeft")
            .Produces(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .WithSummary("Upload left payload")
            .WithDescription("Stores the Base64 encoded left payload for the given id.");

        app.MapPut("/{id}/right", (string id, DiffRequest request, DiffContentStore store) =>
        {
            store.SetRight(id, request.Data);
            return Results.Created($"/v1/diff/{id}/right", null);
        })
            .WithName("PutRight")
            .WithTags("PutRight")
            .Produces(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .WithSummary("Upload right payload")
            .WithDescription("Stores the Base64 encoded right payload for the given id.");

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
        })
            .WithName("GetDiff")
            .WithTags("GetDiff")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithSummary("Compare left and right payloads")
            .WithDescription("""
            Compares the left and right Base64 encoded payloads for the specified id.

            Returns:
            - Equals if payloads are identical
            - SizeDoNotMatch if payload sizes differ
            - ContentDoNotMatch if payloads differ but have equal length
            """);
    }
}