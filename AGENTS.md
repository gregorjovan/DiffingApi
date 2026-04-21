# AGENTS.md

## Project Overview

This repository contains two ASP.NET Core Minimal API solutions for comparing Base64-encoded binary payloads:

- `diffingapi-basic`
  A straightforward in-memory implementation.
- `diffingapi-advanced`
  A layered implementation with SQLite persistence, caching, and keyed in-process locking.

Both solutions expose the same API surface:

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
- SQLite for the advanced solution
- xUnit for unit and integration tests

## Solution Structure

### Basic

- `diffingapi-basic/src/DiffingApi/`
  Basic web API project
- `diffingapi-basic/src/DiffingApi/Contracts/`
  Request DTOs
- `diffingapi-basic/src/DiffingApi/Models/`
  In-memory data models and response models
- `diffingapi-basic/src/DiffingApi/Services/`
  In-memory storage and diff calculation logic
- `diffingapi-basic/src/DiffingApi/Endpoints/`
  Minimal API endpoint mapping
- `diffingapi-basic/tests/DiffingApi.UnitTests/`
  Unit tests for internal logic
- `diffingapi-basic/tests/DiffingApi.IntegrationTests/`
  End-to-end API tests

### Advanced

- `diffingapi-advanced/src/DiffingApi.Advanced.Api/`
  API project and endpoint registration
- `diffingapi-advanced/src/DiffingApi.Advanced.Application/`
  Application services, abstractions, and diff workflow
- `diffingapi-advanced/src/DiffingApi.Advanced.Domain/`
  Domain-level models
- `diffingapi-advanced/src/DiffingApi.Advanced.Infrastructure/`
  SQLite persistence and infrastructure wiring
- `diffingapi-advanced/tests/DiffingApi.UnitTests/`
  Unit tests for diff logic, locking, and application services
- `diffingapi-advanced/tests/DiffingApi.IntegrationTests/`
  End-to-end API tests

## Architecture Notes

- Endpoint registration is centralized in each API project's `Endpoints/ApplicationEndpoints.cs`.
- `Program.cs` should stay focused on service registration and app startup.
- Uploaded payloads are decoded from Base64 during `PUT`, not during `GET`.
- The basic solution keeps decoded `byte[]` values in memory.
- The advanced solution uses SQLite as the source of truth and keeps a short-lived in-memory cache for reads.
- `DiffCalculator` is intentionally static because it is stateless.

## Implementation Guidelines

- Keep changes minimal and aligned with the existing style of the target solution.
- Preserve the current response contract unless tests or requirements call for a change.
- Keep endpoint routes unchanged unless the task explicitly requires route changes.
- Prefer simple inline validation for request checks unless the project clearly evolves beyond that need.
- When changing behavior, update tests and documentation together.
- Keep `basic` straightforward and assignment-friendly.
- Put extra architectural complexity, persistence, or infrastructure features into `advanced`, not `basic`.

## Testing Expectations

Before finishing a change, run the relevant tests for the solution you changed.

For the basic solution:

```bash
dotnet build diffingapi-basic/DiffingApi.Basic.slnx --configuration Release
dotnet test diffingapi-basic/tests/DiffingApi.UnitTests/DiffingApi.UnitTests.csproj --configuration Release --no-build
dotnet test diffingapi-basic/tests/DiffingApi.IntegrationTests/DiffingApi.IntegrationTests.csproj --configuration Release --no-build
```

For the advanced solution:

```bash
dotnet build diffingapi-advanced/DiffingApi.Advanced.slnx --configuration Release
dotnet test diffingapi-advanced/tests/DiffingApi.UnitTests/DiffingApi.UnitTests.csproj --configuration Release --no-build
dotnet test diffingapi-advanced/tests/DiffingApi.IntegrationTests/DiffingApi.IntegrationTests.csproj --configuration Release --no-build
```

If a transient Windows file lock blocks a build or test run, retry the command once before assuming the code is broken.

The repository includes a GitHub Actions workflow at `.github/workflows/ci.yml` that restores, builds, and tests both solutions on pushes and pull requests targeting `main` and `master`.

## Documentation Expectations

- Keep README examples and assumptions in English.
- If behavior changes, update `README.md` and relevant tests together.
- Make implementation assumptions explicit when they affect API behavior.
