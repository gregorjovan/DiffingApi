# AGENTS.md

## Project Overview

This repository contains a small ASP.NET Core Minimal API for comparing Base64-encoded binary payloads.

Current API surface:

- `PUT /v1/diff/{id}/left`
- `PUT /v1/diff/{id}/right`
- `GET /v1/diff/{id}`

The API returns:

- `Equals`
- `SizeDoNotMatch`
- `ContentDoNotMatch` with contiguous diff ranges

## Tech Stack

- .NET 10
- ASP.NET Core Minimal API
- xUnit for unit and integration tests

## Solution Structure

- `DiffingApi/`
  Main web API project
- `DiffingApi/Contracts/`
  Request DTOs
- `DiffingApi/Models/`
  In-memory data models
- `DiffingApi/Services/`
  In-memory storage and diff calculation logic
- `DiffingApi/Endpoints/`
  Minimal API endpoint mapping
- `DiffingApi.UnitTests/`
  Unit tests for internal logic
- `DiffingApi.IntegrationTests/`
  End-to-end API tests

## Architecture Notes

- Endpoint registration is centralized in `DiffingApi/Endpoints/ApplicationEndpoints.cs`.
- `Program.cs` should stay focused on service registration and app startup.
- Uploaded payloads are decoded from Base64 during `PUT`, not during `GET`.
- The in-memory store keeps decoded `byte[]` values.
- `DiffContentStore` is registered as a singleton and acts as the app's in-memory persistence.
- `DiffCalculator` is intentionally static because it is stateless.

## Implementation Guidelines

- Keep changes minimal and aligned with the existing style.
- Prefer simple inline validation for request checks unless the project clearly evolves beyond that need.
- Keep endpoint routes unchanged unless the task explicitly requires route changes.
- Preserve the current response contract unless tests or requirements call for a change.
- Do not introduce persistence or external dependencies unless explicitly requested.
- When adding logic, prefer extending tests first.

## Testing Expectations

Before finishing a change, run the relevant tests:

```bash
dotnet test DiffingApi.IntegrationTests/DiffingApi.IntegrationTests.csproj
dotnet test DiffingApi.UnitTests/DiffingApi.UnitTests.csproj
```

If a transient Windows file lock blocks a test run, retry the command once before assuming the code is broken.

## Documentation Expectations

- Keep README examples and assumptions in English.
- If behavior changes, update `README.md` and tests together.
- Make implementation assumptions explicit when they affect API behavior.
