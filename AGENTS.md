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
aspire run                      # requires the Aspire CLI: dotnet tool install --global Aspire.Cli
# Note: `dotnet run --project AstraHive.AppHost` does NOT work — the AppHost uses
# AspireUseCliBundle=true, so DCP/dashboard binaries come from the `aspire` CLI, not NuGet packages.

# Tests (Docker must be running: handler tests start a PostgreSQL container)
dotnet test AstraHive.slnx

# EF Core migrations (DbContext in Infrastructure, startup project IdentityService; dotnet-ef is a local tool)
dotnet tool restore
dotnet ef migrations add <Name> --project AH.Infrastructure --startup-project AH.IdentityService --output-dir Persistence/Migrations
dotnet ef database update --project AH.Infrastructure --startup-project AH.IdentityService
# (outside `aspire run` the tools need ConnectionStrings__astrahive set; IdentityService applies migrations on startup in Development)

# OAuth credentials and the Unleash token (once per machine; stored in AppHost user-secrets, never in appsettings)
dotnet user-secrets --project AstraHive.AppHost set "Parameters:google-client-id" "<id>"   # also google-client-secret, yandex-client-id, yandex-client-secret
dotnet user-secrets --project AstraHive.AppHost set "Parameters:unleash-api-token" "<server-side token>"

# Certificates (dev): the Debug build of AstraHive.AppHost runs `tools/AH.CertTool dev` once, creating a root CA (root.pem +
# root.key.pem) in %LOCALAPPDATA%\AstraHive\dev-certs and the Global Administrator's client cert for admin@astrahive.local
# (<email>.pfx, password "astrahive", installed into CurrentUser\My; <email>.pem public). Regenerate: delete the folder and
# rebuild. Another admin email: -p:DevCertUserEmail=<email> AND change devAdminEmail in AppHost.cs.
# Certificates (PROD, by hand, once): writes root + Global Admin files and prints the deployment parameters to fill
dotnet run --project tools/AH.CertTool -- init --out <empty dir> --email <admin email> --name "<admin name>"

# Format check
dotnet format AstraHive.slnx --verify-no-changes

# Frontend
cd ah.webapp && npm install
npm run build:watch             # rebuild into AH.WebApi/wwwroot on change (what `aspire run` does); npm run build = production
npm run lint
npm test                        # Vitest (npm run test:watch while developing)
```

## Solution Layout

Projects sit at the repository root (the `/src/` and `/tests/` folders in `AstraHive.slnx` are solution folders only, not physical directories).

```
AstraHive.slnx
AH.AppDomain/              # Domain: entities, value objects, enums, domain errors. NO dependencies.
AH.Application/            # Use cases (commands/queries + handlers), DTOs, validators, IAppDbContext. → AH.AppDomain
AH.Infrastructure/         # EF Core (AppDbContext, configurations, migrations), external adapters. → AH.Application
AH.WebApi/                 # ASP.NET Core Web API (controllers), auth (cookie + Google/Yandex), serves the SPA (https://localhost:7093), composition root. → ServiceDefaults; gRPC client of IdentityService
AH.Grains.Abstractions/    # Orleans grain interfaces + grain message records ([GenerateSerializer]). → nothing but Orleans SDK
AH.Grains/                 # Grain implementations (thin; delegate to domain/handlers). → Grains.Abstractions, Application, AppDomain
AH.SiloHost/               # Orleans silo host (Sdk.Web for health endpoints), composition root. → Grains, Application, Infrastructure, ServiceDefaults
AH.IdentityService/        # gRPC service (Protos/*.proto) that owns users (GetOrCreateUser) and workspaces; applies migrations in dev. → Application, Infrastructure
ah.webapp/                 # Vue 3 + Vite frontend (JavaScript, oxlint + eslint)
AstraHive.AppHost/         # Aspire AppHost: orchestrates all resources (DB, cache, services, frontend)
AstraHive.ServiceDefaults/ # OpenTelemetry, health checks, service discovery, HTTP resilience
tests/                     # Test projects go here (physical folder, to be created)
tools/AH.CertTool/         # Console: root CA + Global Admin client certificate (`dev`: run by the AppHost build; `init`: PROD, by hand). Links AH.Infrastructure/Certificates/ClientCertificateFactory.cs. → nothing
```

## Dependency Rules (enforced, never violate)

- `AH.AppDomain` references nothing (no EF Core, no ASP.NET, no NuGet packages beyond the BCL).
- `AH.Application` references only `AH.AppDomain`. It may use `Microsoft.EntityFrameworkCore` abstractions for `DbSet<T>` in `IAppDbContext`, but never Npgsql or Infrastructure types.
- `AH.Infrastructure` implements interfaces declared in `AH.Application`.
- `AH.WebApi`, `AH.SiloHost` and `AH.IdentityService` are the composition roots; the ones that touch the database call `AddApplication()` + `AddInfrastructure()`. Controllers call Application handlers (or grains via `IGrainFactory`, or IdentityService via its gRPC client), never `AppDbContext` directly.
- `AH.WebApi` consumes `AH.IdentityService` only through the linked `Protos/user_service.proto` and `Protos/workspace_service.proto` (`GrpcServices="Client"`), never a project reference.
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

### Workspaces (tenancy)

- `Workspace` (`AH.AppDomain/Workspaces`) is the tenant; members have one role each: `Administrator` or `Viewer`. A user can be in many workspaces. Invariant: at least one Administrator per workspace.
- A user with no membership gets a workspace (as Administrator) in `GetOrCreateUser` only while `ah.registration_enabled` is on. Administrators add members by email; an unknown email becomes a pending `User` (`LastLoginAt` null) that the first sign-in with that email activates.
- **Global Administrator + main workspace**: `EnsureGlobalAdministrator` runs on every IdentityService start (config `GlobalAdmin:{Email, Name, Certificate}`), idempotently creating or promoting the one `User.IsGlobalAdministrator`, the main workspace (`Workspace.IsMain`, name "AstraHive"; operators only, never clusters: check `Workspace.CanOwnClusters`) and the registration of the admin's bootstrap certificate. `Workspace.IsProtectedMember`: nobody can demote or remove the Global Administrator from the main workspace. The Global Administrator signs in with a certificate only.
- **Client certificates** (`AH.Application/Certificates`): only the Global Administrator issues, lists, downloads and revokes them, for main-workspace members (`CertificateAccess`). `UserCertificate` rows are the registry certificate sign-in checks (thumbprint = SHA-256 hex, active, not revoked); a certificate never creates a user. The PFX (AES-256, random password returned once, never stored) lives in object storage under `certificates/{userId}/{thumbprint}.pfx`; revoking deletes it. The Global Administrator's last active certificate cannot be revoked.
- **Feature flags** (`FeatureFlags`, read through `IFeatureFlags`, off when unknown or unreachable): `ah.external_login` allows Google/Yandex for everyone but the Global Administrator (otherwise certificate only); `ah.registration_enabled` lets unknown emails register through Google/Yandex and allows `CreateWorkspace`. Enforced in Application handlers; WebApi reads them (`SignInFeatures`) only for `/api/auth/options` and to skip a doomed OAuth challenge.
- Sign-in failures travel as `Error.Code` (`SignInErrors`) in the gRPC `error-code` trailer (`RpcResults`); `SignInProvisioner` maps them to the SPA's `/login?error=` codes.
- **Authorization is checked in Application handlers against the database** (`WorkspaceAccess`), with the caller's id passed as `requester_id` over gRPC. Never put roles or workspace ids in cookie claims. Non-members get NotFound, Viewers get Forbidden on changes.
- Future workspace-owned data (clusters, VMs, tasks) carries a `WorkspaceId` and is loaded through the same membership check.

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
- **`AH.WebApi` serves the SPA** (same origin for the SPA, `/api` and the OAuth callbacks), in every environment from `AH.WebApi/wwwroot` (gitignored) with `UseStaticFiles` + `MapFallbackToFile("index.html")`. There is no Vite dev server and no proxy: under `aspire run` the AppHost runs `npm run build:watch` (`AddJavaScriptApp(..., runScriptName: "build:watch")`), which rebuilds into wwwroot on every change; refresh the browser to see edits (no HMR). Production: a Release build/publish of `AH.WebApi` runs `npm ci` (if needed) + `npm run build` via the `BuildSpa` target in `AH.WebApi.csproj` and ships wwwroot; `ah-webapp` exists only in run mode (`ExecutionContext.IsRunMode`), so `aspire publish` deploys no frontend container. Keep `UseStaticFiles` (not `MapStaticAssets`, whose build-time manifest misses runtime rebuilds). Open the app at **https://localhost:7093**.
- Server data uses TanStack Query wrappers in `src/composables/` (e.g. `useProfile.js`) over `src/services/*Service.js`; Pinia keeps only session state (`authStore`).
- API calls go to relative `/api` (no Vite proxy). Unknown `/api/*` routes return 404 instead of `index.html`.

## Tech Stack

- **.NET 10** / C# 14, SDK 10.0.4xx
- **.NET Aspire 13.6**: local orchestration, service discovery, dashboard (`aspire.config.json` points at the AppHost)
- **ASP.NET Core Controllers** in `AH.WebApi` (chosen convention; keep controllers thin)
- **gRPC** (`Grpc.AspNetCore`) in `AH.IdentityService` (HTTP/2-only endpoints, `http` launch profile). gRPC client addresses use an explicit scheme (`http://ah-identityservice`): `GrpcChannel` throws on Aspire's `https+http://` scheme ("No address resolver configured"); the host is still resolved by service discovery on the client's HttpClient
- **Microsoft Orleans**: `Microsoft.Orleans.Sdk` (Grains.Abstractions, Grains), `Microsoft.Orleans.Server` (SiloHost), `Microsoft.Orleans.Client` (WebApi); localhost clustering for now
- **PostgreSQL** via **EF Core + Npgsql**, provisioned with `AddPostgres(...).AddDatabase(...)` in the AppHost and consumed with `builder.AddNpgsqlDbContext<AppDbContext>(...)` (Aspire integration)
- **HybridCache** (in-memory L1). Redis as L2 can be added later via Aspire without code changes
- **FluentValidation**: request validation
- **ProblemDetails** (RFC 9457) + global `IExceptionHandler` for error responses
- **Observability**: Serilog (configured once in ServiceDefaults `AddSerilogDefaults()`, settings in each host's `Serilog` appsettings section, `writeToProviders` → OpenTelemetry → Aspire dashboard). Code injects `ILogger<T>` via constructor; only hosts and ServiceDefaults reference Serilog
- **Auth**: Google/Yandex (behind `ah.external_login`) and client certificates (Google via `Microsoft.AspNetCore.Authentication.Google`, Yandex via `AspNet.Security.OAuth.Yandex`), handled in `AH.WebApi/Auth`. The session is an HttpOnly cookie (`astrahive.session`, SameSite=Lax); there are no JWTs and no local passwords. On `OnCreatingTicket` WebApi calls IdentityService `GetOrCreateUser` through `SignInProvisioner` (users matched by email; the one email check for every sign-in method: required, valid, and not `email_verified=false` from Google) and puts the internal user id in `NameIdentifier`. Endpoints: `GET /api/auth/login/{provider}?returnUrl=`, `GET /api/auth/options`, `GET /api/auth/me`, `POST /api/auth/logout`. Profile: `GET /api/profile` (fresh from IdentityService `GetUser`) and `PUT /api/profile` (`UpdateUserProfile`; WebApi then reissues the session cookie so `/api/auth/me` shows the new name). Provider sign-in only fills names that are still empty (`User.SyncFromProvider`): a user's own edits (`User.Rename`) always win; the avatar keeps syncing. OAuth callbacks (handled by the OAuth middleware) are `/auth/signin-{google,yandex}` on the WebApi origin: in development `https://localhost:7093/auth/signin-google` and `https://localhost:7093/auth/signin-yandex`. These are registered with the providers, so keep the port pinned (WebApi `https` launch profile, selected via `launchProfileName: "https"` in the AppHost). Credentials come from AppHost secret parameters (`google-client-id`, `google-client-secret`, `yandex-client-id`, `yandex-client-secret`) as `Authentication__{Google,Yandex}__{ClientId,ClientSecret}`. **Client-certificate sign-in** (`GET /api/auth/login/certificate?returnUrl=`): on :7093 it redirects to the dedicated port **7094** (AppHost endpoint `certificate`, `isProxied: false`). That is the only port whose TLS handshake requests a client certificate: Kestrel's `ClientCertificateMode.AllowCertificate` + `OnAuthenticate` turns the request off on other ports. There `CertificateSignIn` runs the Certificate handler (chain to the configured root only, `CustomRootTrust`, clientAuth EKU), takes the email from the SAN and the name from the CN, calls `GetOrCreateUser` through the same `SignInProvisioner` with the certificate's SHA-256 thumbprint (must be a registered, unrevoked `UserCertificate` of that user, else `certificate_unknown`) and issues the same session cookie (cookies ignore the port), then redirects to `AppOrigin`. Config: `Authentication:Certificate:{RootCertificate (PEM path or inline PEM, public only), LoginPort, AppOrigin}`, all set by the AppHost in development; without a root or a port the feature is off (`/login?error=certificate_unavailable`). The root's private key never goes to WebApi.
- **Object storage**: MinIO (AppHost `AddMinioContainer("minio")`, CommunityToolkit Aspire integrations), used by IdentityService only through `IObjectStorage` (`MinioObjectStorage`, bucket `ObjectStorage:Bucket`, default `astrahive`, created on first use)
- **Feature flags**: Unleash (`Unleash.Client`), one `IUnleash` per host from `AddFeatureFlagClient()` in ServiceDefaults (config `Unleash:{ApiUrl, ApiToken}` from AppHost parameters `unleash-api-url`, `unleash-api-token`; without them every flag is off). Server-side (client API) token only; the SPA gets flags from `GET /api/auth/options`
- **Certificate authority**: IdentityService only (`CertificateAuthority:{Certificate, PrivateKey}`, PEM text or path). WebApi gets the root's public PEM only. Run mode: dev files; publish mode: AppHost parameters `certificate-root`, `certificate-root-key`, `global-admin-email`, `global-admin-name`, `global-admin-certificate` (output of `AH.CertTool init`)
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
  AH.Application.Tests/      # exists: domain + Application (validators, handlers against real PostgreSQL)
    Fixtures/PostgresFixture.cs     # assembly fixture: one Testcontainers postgres:17, production migrations applied
    Fixtures/DatabaseTestBase.cs    # base for DB tests: truncates tables per test, CreateDbContext()
    Domain/<Aggregate>/<Entity>Tests.cs
    <Feature>/<Operation>/<Handler|Validator>Tests.cs
  AH.Grains.Tests/           # planned: grain tests with Orleans InProcessTestCluster
  AH.WebApi.Tests/           # planned: integration tests with WebApplicationFactory + Testcontainers PostgreSQL
```

- xUnit v3 on the VSTest adapter (`xunit.v3` + `xunit.runner.visualstudio`), so plain `dotnet test AstraHive.slnx` works. Assertions use xUnit `Assert`.
- AAA pattern, one behavior per test, name: `Method_Scenario_ExpectedResult`.
- Real PostgreSQL via Testcontainers: **never** the EF in-memory provider. DB test classes derive from `DatabaseTestBase` (one non-parallel `Database` collection) and use a fresh `AppDbContext` per act/assert.
- Use `FakeTimeProvider` for time-dependent logic; pass `TestContext.Current.CancellationToken` to async calls.
- Frontend: Vitest + Testing Library, specs co-located as `*.spec.js` (see `ah.webapp/CLAUDE.md`).

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
- References Serilog (or its static `Log`) outside host `Program.cs` / ServiceDefaults: inject `ILogger<T>`
- Adds MediatR, AutoMapper, MassTransit v9+, or FluentAssertions v8+ (commercial licenses)
- Puts OAuth client ids/secrets, the Unleash token or the root CA key in appsettings or source; use the AppHost secret parameters
