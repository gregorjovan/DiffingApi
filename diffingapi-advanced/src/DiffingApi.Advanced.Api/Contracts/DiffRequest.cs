namespace DiffingApi.Advanced.Api.Contracts;

/// <summary>
/// Request body for uploading one side of a diff payload.
/// </summary>
public sealed record DiffRequest(string Data);
