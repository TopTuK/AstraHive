# AGENTS.md

Instructions for AI coding agents working in this repository. Read this file fully before writing code.

## Purpose

AstraHive is a **virtual machine orchestration system**: users connect to virtual machines over SSH to perform maintenance operations, VMs are grouped into clusters, and users can schedule recurrent tasks.

Technically: a .NET 10 distributed application orchestrated with .NET Aspire, with a Microsoft Orleans silo for stateful, single-threaded entity processing. It uses Clean Architecture for a moderately complex domain: real business rules, but mostly workflows over data rather than deep aggregate invariants. Solo developer, so favor simplicity over ceremony — don't add an abstraction until a second use case needs it.

Status: early skeleton. The layer projects exist but are empty and not yet wired together.

## Commands

```bash
# Build everything
dotnet build AstraHive.slnx

# Run the whole system (Aspire dashboard + all services)
aspire run                      # or: dotnet run --project AstraHive.AppHost

# Tests
dotnet test AstraHive.slnx

# EF Core migrations (DbContext in Infrastructure, startup project WebApi)
dotnet ef migrations add <Name> --project AH.Infrastructure --startup-project AH.WebApi --output-dir Persistence/Migrations
dotnet ef database update --project AH.Infrastructure --startup-project AH.WebApi

# Format check
dotnet format AstraHive.slnx --verify-no-changes

# Frontend
cd ah.webapp && npm install && npm run dev
npm run lint
```

## Solution Layout

Projects sit at the repository root (the `/src/` and `/tests/` folders in `AstraHive.slnx` are solution folders only, not physical directories).

```
AstraHive.slnx
AH.AppDomain/              # Domain: entities, value objects, enums, domain errors. NO dependencies.
AH.Application/            # Use cases (commands/queries + handlers), DTOs, validators, IAppDbContext. → AH.AppDomain
AH.Infrastructure/         # EF Core (AppDbContext, configurations, migrations), external adapters. → AH.Application
AH.WebApi/                 # ASP.NET Core Web API (controllers), composition root. → AH.Application, AH.Infrastructure
AH.Grains.Abstractions/    # Orleans grain interfaces + grain message records ([GenerateSerializer]). → nothing but Orleans SDK
AH.Grains/                 # Grain implementations (thin; delegate to domain/handlers). → Grains.Abstractions, Application, AppDomain
AH.SiloHost/               # Orleans silo host (Sdk.Web for health endpoints), composition root. → Grains, Application, Infrastructure, ServiceDefaults
AH.IdentityService/        # gRPC service (Protos/*.proto); identity/auth service, design TBD
ah.webapp/                 # Vue 3 + Vite frontend (JavaScript, oxlint + eslint)
AstraHive.AppHost/         # Aspire AppHost: orchestrates all resources (DB, cache, services, frontend)
AstraHive.ServiceDefaults/ # OpenTelemetry, health checks, service discovery, HTTP resilience
tests/                     # Test projects go here (physical folder, to be created)
```

## Dependency Rules (enforced, never violate)

- `AH.AppDomain` references nothing (no EF Core, no ASP.NET, no NuGet packages beyond the BCL).
- `AH.Application` references only `AH.AppDomain`. It may use `Microsoft.EntityFrameworkCore` abstractions for `DbSet<T>` in `IAppDbContext`, but never Npgsql or Infrastructure types.
- `AH.Infrastructure` implements interfaces declared in `AH.Application`.
- `AH.WebApi` and `AH.SiloHost` are the composition roots; both call `AddApplication()` + `AddInfrastructure()`. Controllers call Application handlers (or grains via `IGrainFactory`), never `AppDbContext` directly.
- **Orleans stays out of the core:** `AH.AppDomain` and `AH.Application` never reference Orleans packages or attributes.
- Orleans clients (`AH.WebApi`) reference only `AH.Grains.Abstractions`, never `AH.Grains`.
- Each layer exposes a `DependencyInjection.cs` with an `Add<Layer>(this IServiceCollection)` / `IHostApplicationBuilder` extension.
- After any structural change, verify the project reference graph: no rule violations, no circular dependencies.

## Architecture Conventions

### Application Layer

- One folder per feature: `AH.Application/<Feature>/<Operation>/` containing the command/query record, handler, validator, and response DTO.
- **Plain handler classes** (no mediator library), registered in DI and injected into controllers. Revisit only if cross-cutting pipeline behaviors become necessary.
- Data access through `IAppDbContext` (exposes `DbSet<T>` + `SaveChangesAsync`). **No repository-per-entity**: `DbSet` is already a repository.
- Handlers return `Result<T>` for expected failures (not found, validation, conflict); exceptions are for truly exceptional cases only.
- Validation with FluentValidation in the Application layer.

### Orleans

- **Hosting:** dedicated silo in `AH.SiloHost` (`builder.UseOrleans(...)`); `AH.WebApi` is a client (`builder.UseOrleansClient(...)`).
- **Clustering (current):** `UseLocalhostClustering()` + in-memory grain storage/reminders for development. Production clustering is **not decided yet**. Don't add Redis/AdoNet providers without asking. When it's chosen, wire it through Aspire (`AddOrleans(...)` in AppHost + `WithReference`).
- **Domain data persistence:** grains load and save through `IAppDbContext` (EF Core/PostgreSQL), so data stays relational and queryable. Do **not** use `IPersistentState<T>` for domain data; Orleans storage is only for transient/technical grain state.
- **DbContext lifetime:** grain activations are long-lived and have no request scope. **Never inject a scoped `IAppDbContext` or handler directly into a grain.** Create a scope per grain call (`IServiceScopeFactory`) and resolve the handler from it, or use a DbContext factory abstraction for a short-lived context per operation.
- **Thin grains:** a grain gives an entity a single-threaded address. Business rules live in `AH.AppDomain` entities and `AH.Application` handlers.
- **Serialization:** only types in `AH.Grains.Abstractions` carry `[GenerateSerializer]` / `[Id(n)]`. Map to/from Application DTOs; never put Orleans attributes on domain entities or Application DTOs.
- Grain interfaces: `I<Name>Grain : IGrainWithGuidKey` (or a string/int key as fits); every method returns `Task`/`ValueTask`, and is async all the way.
- Prefer `[Reentrant]` / `[ReadOnly]` only with a clear reason; never block inside a grain (`.Result`, `Thread.Sleep`, sync I/O).

### Controllers

- `[ApiController]`, attribute routing (`[Route("api/[controller]")]`), one controller per resource.
- Return `ActionResult<TResponse>` / typed results; declare `[ProducesResponseType]` for OpenAPI.
- Controllers only: bind input → call handler → map `Result<T>` to HTTP response. No business logic, no `DbContext`.
- Never return domain entities; always response DTOs.

### Frontend (`ah.webapp`)

- Vue 3 `<script setup>` (JavaScript), Tailwind CSS v4, Pinia, Vue Router, vue-i18n, shadcn-vue (reka-ui) + lucide icons. Follow `ah.webapp/CLAUDE.md`.
- `App.vue` picks `UnauthorizedLayout` or `AuthorizedLayout` (`src/layout/`) from `authStore.isAuthenticated`. Routes declare `meta: { title, requiresAuth }`; views are lazy-loaded.
- Design tokens live in `src/main.css` (shadcn semantic names + `sidebar-*`, `terminal-*`, `status-*`). Dark mode is `html[data-theme="dark"]`, switched by `src/theme`. Never hardcode colors in components.
- Every UI string goes through i18n (`src/i18n/locales/en.json` + `ru.json`, keep both in sync).
- shadcn-vue components live in `src/components/ui/` and are written by hand: the `shadcn-vue add` CLI fails in this environment (npm `allow-scripts` error).
- API calls go to relative `/api`; Vite proxies them to `ah-webapi` through Aspire service discovery env vars. The frontend runs through the AppHost (`AddViteApp`).

## Tech Stack

- **.NET 10** / C# 14, SDK 10.0.4xx
- **.NET Aspire 13.6**: local orchestration, service discovery, dashboard (`aspire.config.json` points at the AppHost)
- **ASP.NET Core Controllers** in `AH.WebApi` (chosen convention; keep controllers thin)
- **gRPC** (`Grpc.AspNetCore`) in `AH.IdentityService`
- **Microsoft Orleans**: `Microsoft.Orleans.Sdk` (Grains.Abstractions, Grains), `Microsoft.Orleans.Server` (SiloHost), `Microsoft.Orleans.Client` (WebApi); localhost clustering for now
- **PostgreSQL** via **EF Core + Npgsql**, provisioned with `AddPostgres(...).AddDatabase(...)` in the AppHost and consumed with `builder.AddNpgsqlDbContext<AppDbContext>(...)` (Aspire integration)
- **HybridCache** (in-memory L1). Redis as L2 can be added later via Aspire without code changes
- **FluentValidation**: request validation
- **ProblemDetails** (RFC 9457) + global `IExceptionHandler` for error responses
- **Observability**: `ILogger` structured logging + OpenTelemetry via ServiceDefaults → Aspire dashboard
- **Auth**: not decided yet. Do not add auth scaffolding until it is designed (likely owned by `AH.IdentityService`)
- **Messaging**: none. Services communicate synchronously over gRPC/HTTP with Aspire service discovery
- **Frontend**: Vue 3, Vite 8, Node `^22.18 || >=24.12`
- **Testing**: xUnit v3, WebApplicationFactory, Testcontainers.PostgreSql, Verify

## Coding Standards

- **C# 14 features**: primary constructors, collection expressions, `field` keyword, records, pattern matching
- **File-scoped namespaces**, always; namespaces match project + folder (`AH.Application.Orders.CreateOrder`)
- **`var` for obvious types**; explicit types when not clear from context
- **Naming**: PascalCase public members, `_camelCase` private fields, `Async` suffix on async methods
- **`sealed` by default** for classes not designed for inheritance
- **`CancellationToken`** on every async method, propagated through to EF Core
- **No regions**; comment "why", never "what"
- Entities have behavior (methods enforcing invariants, private setters). Avoid anemic property bags where rules exist.

## Testing

```
tests/
  AH.Application.Tests/      # Unit tests for domain + handler logic
  AH.Grains.Tests/           # Grain tests with Orleans InProcessTestCluster
  AH.WebApi.Tests/           # Integration tests: WebApplicationFactory + Testcontainers PostgreSQL
    Fixtures/ApiFixture.cs
    <Feature>/<Operation>Tests.cs
```

- AAA pattern, one behavior per test, name: `Method_Scenario_ExpectedResult`.
- Real PostgreSQL via Testcontainers: **never** the EF in-memory provider.
- Use `FakeTimeProvider` for time-dependent logic.

## Workflow

- **Plan first**: for any non-trivial task (3+ steps or architecture decisions), state a plan before implementing.
- **Verify before done**: `dotnet build` + `dotnet test`, then check the changed files compile cleanly. Ask: "Would a staff engineer approve this?"
- **Every new resource goes through the AppHost**: databases, caches, the frontend, new services, Orleans cluster config. No hardcoded connection strings or URLs.
- **Stop and re-plan** if an approach goes sideways; don't tunnel-vision on a broken path.
- **Use symbol-level navigation** (find symbol / references / diagnostics) instead of raw text search when your tooling provides it — it's cheaper and more precise.
- **Keep this file current**: when a convention changes or a correction recurs, record it here. This file is the shared memory for every agent working on AstraHive.

## Anti-patterns

Do NOT generate code that:

- Adds references that break the dependency rules (e.g. Application → Infrastructure, AppDomain → anything)
- Puts business logic in controllers or accesses `AppDbContext` from `AH.WebApi` controllers
- Creates a repository interface per entity: use `IAppDbContext`
- Hardcodes connection strings or service URLs; use Aspire references + service discovery
- Uses `DateTime.Now` / `DateTime.UtcNow`: inject `TimeProvider`
- Creates `new HttpClient()`: use `IHttpClientFactory` / typed clients (resilience comes from ServiceDefaults)
- Uses `async void`, `.Result`, or `.Wait()`
- Returns domain entities from controllers or gRPC services, or passes them across grain boundaries
- Injects scoped services (`IAppDbContext`, handlers) directly into grain constructors
- Adds Orleans packages/attributes to `AH.AppDomain` or `AH.Application`
- Stores domain data in `IPersistentState<T>` instead of PostgreSQL via `IAppDbContext`
- Uses the EF in-memory provider for tests
- Catches bare `Exception`: catch specific types, let the global handler catch the rest
- Uses string interpolation in log messages: use structured message templates
- Adds MediatR, AutoMapper, MassTransit v9+, or FluentAssertions v8+ (commercial licenses)
- Scaffolds authentication before the auth design is decided
