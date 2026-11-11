# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

"Electronic" is a .NET 10 product-inventory backend built in Clean Architecture layers. It uses SQL Server through Dapper and stored procedures, and has two hosts over the same services: a REST API (`Electronic.Api`) and an MCP server (`Electronic.Mcp`) that exposes the services as tools to Claude.

## Commands

```bash
dotnet build Electronic.slnx
dotnet test                                    # runs Electronic.Application.Tests (xUnit + Moq)
dotnet test --filter "FullyQualifiedName~ProductServiceTests.Create_WhenValid_ReturnsOk"   # single test
dotnet run --project Electronic.Api            # http://localhost:5285 (https profile: https://localhost:7110)
dotnet run --project Electronic.Mcp            # stdio MCP server, normally launched by the MCP client
```

In Development the API serves OpenAPI at `/openapi/v1.json` and the Scalar UI at `/scalar`.

**Database:** both hosts read the connection string `ConnectionStrings:DBConnection`.
- In `Electronic.Api/appsettings.json` it is an empty string, so the API starts but fails on the first DB call. Set it locally, for example with the env var `ConnectionStrings__DBConnection` or in `appsettings.Development.json`. The null check in `AddInfrastructure` only catches a missing key, not an empty value.
- `Electronic.Mcp/appsettings.json` holds a machine-specific local SQLEXPRESS connection string.
- The database schema and stored procedures are **not in this repo**. The repository calls `SP_Product_GetAll`, `SP_Product_GetBySku`, `SP_Product_Exists`, `SP_Product_Insert`, `SP_Product_Update` and `SP_Product_Delete`.

## Architecture

Project dependencies:

```
Domain  ←  Application  ←  Infrastructure
                 ↑               ↑
          Api ───┴───────────────┘
          Mcp ───┴───────────────┘
```

- The folder is spelled **`Electronic.Infraestructure/`**, but its csproj, assembly and namespaces are `Electronic.Infrastructure`.
- **Domain**: entities (`ProductModel`) and repository interfaces (`IProductRepository`). It has no dependencies.
- **Application**: services, DTOs, mapping extension methods (`ToDto` / `ToModel` / `ToDtoList`), static validators and `ApiResponse<T>`.
- **Infrastructure**: Dapper repositories. Every method opens its own connection from the singleton `IDbConnectionFactory` and runs a stored procedure via `CommandDefinition` with `CommandType.StoredProcedure`, passing the `CancellationToken` through.
- **DI**: each layer has a `DependencyInjection.cs` with `AddApplication()` or `AddInfrastructure(IConfiguration)`. Register new services and repositories there. Both hosts call these, so registrations stay in sync between the API and MCP.

### Response/error convention

- Services never throw. Each method wraps its work in `try/catch` and returns `ApiResponse<T>` (`Code`, `Message`, `Result`), built with `ApiResponse<T>.Success(...)` or `ApiResponse<T>.Fail(HttpStatusCode, message)`.
- Any exception becomes a 500 with the generic `Messages.InternalError`, and nothing is logged (no `ILogger` is injected).
- Controllers are thin pass-throughs: `return StatusCode(response.Code, response);`.
- Validation is hand-written. `ProductValidators.Validate(dto)` returns the first error message, or `null` if the DTO is valid.
- User-facing strings are `const` format strings in `Application/Common/Constants/Messages.cs`, used with `string.Format(Messages.X, sku)`. Infrastructure keeps its own `Constants/Messages.cs` and `Config.cs`.
- Status codes the tests rely on: `GetBySku` on a missing SKU returns **404**, but `Update`/`Delete` on a missing SKU and `Create` on a duplicate SKU return **400**.

### API host

`Program.cs` applies the `PerIp` rate-limit policy to **all** controllers with `MapControllers().RequireRateLimiting(...)`. The policy is a fixed window of 100 requests per minute per remote IP, with no queue. A rejected request gets a 429 with an `ApiResponse<string>` JSON body. The policy is defined in `Extensions/RateLimitingExtensions.cs`.

### MCP host

- It uses the `ModelContextProtocol` package over the **stdio** transport, so stdout is the protocol channel. Never write to stdout (`Console.WriteLine`, etc.); logging is routed to stderr.
- Tools are discovered with `WithToolsFromAssembly()`. They are classes marked `[McpServerToolType]` whose methods are marked `[McpServerTool, Description("...")]` (see `Mcp/Tools.cs`). The `Description` is what the model sees.
- Tools should delegate to Application services rather than to repositories.
- `ContentRootPath = AppContext.BaseDirectory`, and the csproj copies `appsettings.json` to the output folder, so config is found whatever the working directory.

### Other projects

- `Electronic.Web`: an untouched ASP.NET MVC template that references none of the other layers.
- `Electronic.Tests`: an empty placeholder. It is **not in `Electronic.slnx`**, so `dotnet test` and CI never run it. The real tests are in `Electronic.Application.Tests`, which unit-tests services by mocking `IProductRepository` with Moq.
- `Electronic.Api/WeatherForecast.cs` and `Electronic.Api.http` are template leftovers. The `.http` file points to a `/weatherforecast` endpoint that doesn't exist.

## CI / Release

`.github/workflows/CI.yml` runs `restore`, then `build -c Release`, then `test` on every PR to `main` and every push to `main`. A **push to `main` also publishes `Electronic.Api` and creates a GitHub release** tagged `v1.0.<run_number>`, so every merge to main ships a release.

## Workflow conventions

- Work happens on `feature/<desc>` or `feat/<desc>` branches merged into `main` through a PR.
- Commit messages use a `feat:` prefix and are written in Spanish or English.
- The project uses Spec Kit (`.specify/`, v1.4.0, PowerShell scripts) with the `/speckit-*` skills in `.claude/skills/`. Feature specs, plans and tasks are generated through these skills.
- Project principles belong in `.specify/memory/constitution.md`, managed with `/speckit-constitution`.
