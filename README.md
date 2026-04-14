# DiffingApi

REST API for comparing Base64-encoded binary data and returning high-level diff information.

## Requirements Covered

- `PUT /v1/diff/{id}/left`
- `PUT /v1/diff/{id}/right`
- `GET /v1/diff/{id}`
- `Equals` result when both payloads are identical
- `SizeDoNotMatch` result when payload lengths differ
- `ContentDoNotMatch` result with contiguous `offset`/`length` ranges when payload lengths are equal but contents differ
- integration tests for endpoint behavior
- unit tests for the internal diff calculation logic

## Running the Application

```bash
dotnet run --project DiffingApi/DiffingApi.csproj
```

By default, the API is available on the local ASP.NET Core development URL shown in the console output.

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
- The diff algorithm is intentionally simple and returns contiguous mismatch ranges.
  It does not attempt to calculate an optimal edit script.
- Actual differing bytes are not returned because the assignment only requires offsets and lengths.
- Invalid Base64 is treated as a bad request and returns `400 Bad Request`.

## Test Projects

- `DiffingApi.IntegrationTests`
  Covers the API behavior end-to-end.
- `DiffingApi.UnitTests`
  Covers the internal diff range calculation logic.
