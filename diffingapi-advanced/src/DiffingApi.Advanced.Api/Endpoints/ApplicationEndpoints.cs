using DiffingApi.Advanced.Api.Contracts;
using DiffingApi.Advanced.Application.Abstractions;
using DiffingApi.Advanced.Application.Services;

namespace DiffingApi.Advanced.Api.Endpoints;

/// <summary>
/// Registers the diff API endpoints.
/// </summary>
public static class ApplicationEndpoints
{
    /// <summary>
    /// Maps the diff API endpoints under <c>/v1/diff</c>.
    /// </summary>
    public static void MapApplicationEndpoints(this WebApplication app)
    {
        var diffGroup = app.MapGroup("/v1/diff")
            .WithTags("DiffingApi");

        diffGroup.MapPut("/{id}/left", (string id, DiffRequest? request, IDiffContentStore store) =>
        {
            if (request is null || string.IsNullOrEmpty(request.Data))
                return Results.Problem(statusCode: StatusCodes.Status400BadRequest);

            byte[] leftBytes;

            try
            {
                leftBytes = Convert.FromBase64String(request.Data);
            }
            catch (FormatException)
            {
                return Results.Problem(statusCode: StatusCodes.Status400BadRequest);
            }

            store.SetLeft(id, leftBytes);
            return Results.Created($"/v1/diff/{id}/left", null);
        })
            .WithName("PutLeft")
            .WithTags("PutLeft")
            .Produces(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .WithSummary("Upload left payload")
            .WithDescription("Stores the Base64 encoded left payload for the given id.");

        diffGroup.MapPut("/{id}/right", (string id, DiffRequest? request, IDiffContentStore store) =>
        {
            if (request is null || string.IsNullOrEmpty(request.Data))
                return Results.Problem(statusCode: StatusCodes.Status400BadRequest);

            byte[] rightBytes;

            try
            {
                rightBytes = Convert.FromBase64String(request.Data);
            }
            catch (FormatException)
            {
                return Results.Problem(statusCode: StatusCodes.Status400BadRequest);
            }

            store.SetRight(id, rightBytes);
            return Results.Created($"/v1/diff/{id}/right", null);
        })
            .WithName("PutRight")
            .WithTags("PutRight")
            .Produces(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .WithSummary("Upload right payload")
            .WithDescription("Stores the Base64 encoded right payload for the given id.");

        diffGroup.MapGet("/{id}", (string id, IDiffContentStore store) =>
        {
            if (!store.TryGet(id, out var entry) || entry?.Left is null || entry.Right is null)
                return Results.NotFound();

            var leftBytes = entry.Left;
            var rightBytes = entry.Right;

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
