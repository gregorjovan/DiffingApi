# DiffingApi

REST API for comparing Base64-encoded binary data and returning high-level diff information.

This repository contains two variants of the solution:

- `diffingapi-basic`
  A straightforward in-memory implementation.
- `diffingapi-advanced`
  A layered implementation with SQLite persistence, caching, and keyed in-process locking.

## Requirements Covered

- `PUT /v1/diff/{id}/left`
- `PUT /v1/diff/{id}/right`
- `GET /v1/diff/{id}`
- `GET /v1/diff/{id}/status` for the advanced solution's background diff processing status
- `Equals` result when both payloads are identical
- `SizeDoNotMatch` result when payload lengths differ
- `ContentDoNotMatch` result with contiguous `offset`/`length` ranges when payload lengths are equal but contents differ
- integration tests for endpoint behavior
- unit tests for the internal diff calculation logic

## Repository Layout

- `diffingapi-basic`
  Basic solution with:
  - `src/DiffingApi`
  - `tests/DiffingApi.UnitTests`
  - `tests/DiffingApi.IntegrationTests`
- `diffingapi-advanced`
  Advanced solution with:
  - `src/DiffingApi.Advanced.Api`
  - `src/DiffingApi.Advanced.Application`
  - `src/DiffingApi.Advanced.Domain`
  - `src/DiffingApi.Advanced.Infrastructure`
  - `tests/DiffingApi.UnitTests`
  - `tests/DiffingApi.IntegrationTests`

## Running the Applications

### Basic solution

```bash
dotnet run --project diffingapi-basic/src/DiffingApi/DiffingApi.Basic.csproj
```

### Advanced solution

```bash
dotnet run --project diffingapi-advanced/src/DiffingApi.Advanced.Api/DiffingApi.Advanced.Api.csproj
```

By default, each application is available on the local ASP.NET Core development URL shown in the console output.

Swagger UI is available at:

```text
/
```

## API Usage

### Upload left payload

```http
PUT /v1/diff/{id}/left
Content-Type: application/json

{
  "data": "AAAAAA=="
}
```

Response:

```http
201 Created
```

### Upload right payload

```http
PUT /v1/diff/{id}/right
Content-Type: application/json

{
  "data": "AQABAQ=="
}
```

Response:

```http
201 Created
```

### Compare payloads

```http
GET /v1/diff/{id}
```

Possible responses:

```json
{ "diffResultType": "Equals" }
```

```json
{ "diffResultType": "SizeDoNotMatch" }
```

```json
{
  "diffResultType": "ContentDoNotMatch",
  "diffs": [
    { "offset": 0, "length": 1 },
    { "offset": 2, "length": 2 }
  ]
}
```

If one side is missing, the API returns:

```http
404 Not Found
```

### Poll background diff status

The advanced solution also queues a background diff job automatically when both sides have been uploaded. New clients can poll the non-blocking status endpoint:

```http
GET /v1/diff/{id}/status
```

Possible in-progress responses:

```json
{ "status": "Pending" }
```

```json
{ "status": "Processing" }
```

Completed responses keep the existing result fields:

```json
{
  "status": "Completed",
  "diffResultType": "ContentDoNotMatch",
  "diffs": [
    { "offset": 0, "length": 1 },
    { "offset": 2, "length": 2 }
  ]
}
```

Failed responses include a reason:

```json
{
  "status": "Failed",
  "reason": "Both left and right payloads are required."
}
```

If the request body is missing, `data` is null or empty, or `data` is not valid Base64, the API returns:

```http
400 Bad Request
```

## Assumptions and Implementation Choices

- Payloads are stored in memory only. Data is lost when the application stops.
- Base64 payloads are decoded during `PUT`, not during `GET`.
  This keeps comparison requests simple and avoids decoding the same payload repeatedly.
- The API stores only the latest uploaded left and right payload for a given `{id}`.
  Uploading the same side again replaces the previous value.
- Requests may arrive in any order.
  The API compares whatever left and right payloads are currently stored for the given `{id}`.
- If the same side is uploaded multiple times, the latest successfully stored value is used.
  In other words, the implementation follows a last-write-wins approach per side.
- Validation is intentionally implemented inline inside the endpoint handlers.
  For the current assignment scope, introducing a separate validation library such as FluentValidation would add unnecessary abstraction.
- The diff algorithm is intentionally simple and returns contiguous mismatch ranges.
  It does not attempt to calculate an optimal edit script.
- Actual differing bytes are not returned because the assignment only requires offsets and lengths.
- Invalid Base64 is treated as a bad request and returns `400 Bad Request`.
- The advanced solution keeps `GET /v1/diff/{id}` synchronous for backward compatibility.
  The opt-in `GET /v1/diff/{id}/status` endpoint exposes the background processing status and completed result.
- Advanced background diff processing uses a global FIFO in-memory queue.
  `DiffProcessing:MaxConcurrency` in `appsettings.json` controls how many diff jobs may run at the same time across all IDs.
- Advanced diff processing status and completed results are stored in SQLite on the same `DiffPairs` row as the left and right payloads.
  Re-uploading either side clears the previous status/result and, once both sides exist, updates the row to `Pending` for a new background job.
- Per-ID locking still protects updates and reads for the same ID, while the global concurrency limit controls total background diff throughput.
- Payload size limits and eviction policies were intentionally not added.
  Because the assignment uses an in-memory store and does not require production-scale persistence or retention controls, adding those mechanisms would introduce extra complexity beyond the current scope.

## Building and Testing

### Build basic

```bash
dotnet build diffingapi-basic/DiffingApi.Basic.slnx --configuration Release
```

### Build advanced

```bash
dotnet build diffingapi-advanced/DiffingApi.Advanced.slnx --configuration Release
```

### Test basic

```bash
dotnet test diffingapi-basic/tests/DiffingApi.UnitTests/DiffingApi.UnitTests.csproj --configuration Release
dotnet test diffingapi-basic/tests/DiffingApi.IntegrationTests/DiffingApi.IntegrationTests.csproj --configuration Release
```

### Test advanced

```bash
dotnet test diffingapi-advanced/tests/DiffingApi.UnitTests/DiffingApi.UnitTests.csproj --configuration Release
dotnet test diffingapi-advanced/tests/DiffingApi.IntegrationTests/DiffingApi.IntegrationTests.csproj --configuration Release
```

### Test projects

- `diffingapi-basic/tests/DiffingApi.UnitTests`
  Covers the internal diff range calculation logic for the basic solution.
- `diffingapi-basic/tests/DiffingApi.IntegrationTests`
  Covers the basic API behavior end-to-end.
- `diffingapi-advanced/tests/DiffingApi.UnitTests`
  Covers the internal diff logic, locking, and service behavior for the advanced solution.
- `diffingapi-advanced/tests/DiffingApi.IntegrationTests`
  Covers the advanced API behavior end-to-end.

## Continuous Integration

The repository includes a GitHub Actions workflow at `.github/workflows/ci.yml`.

It runs on pushes and pull requests targeting `main` and `master`, and performs:

- dependency restore
- basic solution build and test execution
- advanced solution build and test execution
