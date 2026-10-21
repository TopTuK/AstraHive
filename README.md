# Astra Hive

**A virtual machine orchestration system** built on .NET 10, Microsoft Orleans, and .NET Aspire.

Astra Hive lets operators manage fleets of virtual machines grouped into clusters: connect to any VM over SSH to perform maintenance operations, and schedule recurrent tasks that run automatically across the fleet.

## Core Capabilities

- **Virtual machine management** — a registry of VMs with their connection details and state, each processed by a dedicated Orleans grain for single-threaded, consistent handling.
- **SSH maintenance operations** — users connect to a VM via SSH from a central place and run maintenance operations without juggling terminals or credentials.
- **Clusters** — VMs are grouped into clusters so operations can target a single machine or an entire group.
- **Recurrent scheduled tasks** — users schedule maintenance tasks (e.g., nightly cleanups, patch checks) that run on a recurring schedule against a VM or cluster, backed by Orleans timers/reminders.

> **Status:** early skeleton. The solution layout and tooling are in place, but the domain features above are not implemented yet. See [Current State](#current-state).

## Architecture

How the processes, the identity flow, and the layer rules fit together is described in [architecture.md](architecture.md), with diagrams of the running system and of the Orleans orchestration that is not built yet.

## Solution Layout

| Project | Purpose |
| --- | --- |
| `AH.AppDomain/` | Domain: entities, value objects, enums, domain errors. No dependencies. |
| `AH.Application/` | Use cases (commands/queries + handlers), DTOs, validators, `IAppDbContext`. |
| `AH.Infrastructure/` | EF Core (`AppDbContext`, configurations, migrations), external adapters. |
| `AH.WebApi/` | ASP.NET Core Web API (controllers); serves the SPA; gRPC client of IdentityService. |
| `AH.Grains.Abstractions/` | Orleans grain interfaces + serializable grain messages. |
| `AH.Grains/` | Grain implementations (thin; delegate to domain/application). |
| `AH.SiloHost/` | Future Orleans silo host. Health endpoints only today. |
| `AH.IdentityService/` | gRPC service that owns users and workspaces. See [architecture.md](architecture.md). |
| `ah.webapp/` | Vue 3 + Vite frontend. |
| `AstraHive.AppHost/` | Aspire AppHost: orchestrates all resources. |
| `AstraHive.ServiceDefaults/` | OpenTelemetry, health checks, service discovery, HTTP resilience. |

Projects sit at the repository root; `/src/` and `/tests/` in `AstraHive.slnx` are solution folders only.

## Tech Stack

- **Backend:** .NET 10 / C# 14, ASP.NET Core controllers, Microsoft Orleans
- **Database:** PostgreSQL via EF Core + Npgsql (Aspire integration)
- **Caching:** HybridCache (in-memory L1; Redis L2 can be added via Aspire later)
- **Validation & errors:** FluentValidation, ProblemDetails (RFC 9457)
- **Identity:** Google, Yandex and client-certificate sign-in in `AH.WebApi`; users and workspaces owned by gRPC `AH.IdentityService`
- **Observability:** OpenTelemetry → Aspire dashboard
- **Frontend:** Vue 3, Vite 8, Node `^22.18 || >=24.12`
- **Testing:** xUnit v3, WebApplicationFactory, Testcontainers PostgreSQL, Verify

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (10.0.4xx)
- [Aspire CLI](https://learn.microsoft.com/dotnet/aspire/fundamentals/setup-tooling) (`dotnet tool install -g aspire.cli`)
- Node.js `^22.18 || >=24.12` (for the frontend)
- Docker (PostgreSQL and MinIO via Aspire)

### Run

```bash
# Everything: Aspire dashboard + all services
aspire run
```

`dotnet run --project AstraHive.AppHost` does not work: the AppHost takes the dashboard binaries from the Aspire CLI. The Aspire dashboard shows every resource's logs, traces, and metrics.

### Configuration (once per machine)

Secrets live in the AppHost user-secrets, never in appsettings:

```bash
dotnet user-secrets --project AstraHive.AppHost set "Parameters:unleash-api-token" "<Unleash server-side token>"
dotnet user-secrets --project AstraHive.AppHost set "Parameters:google-client-id" "<id>"   # also google-client-secret, yandex-client-id, yandex-client-secret
```

Feature flags come from Unleash (`unleash-api-url`, default `https://features.s-sidorov.ru/api/`). Create these toggles in the Unleash project; both are off by default, and also read as off when Unleash is unreachable:

| Flag | On |
| --- | --- |
| `ah.external_login` | Google/Yandex sign-in for everyone except the Global Administrator (verified email required). Off: client certificates only. |
| `ah.registration_enabled` | Unknown emails register through Google/Yandex and get their own workspace; users can create more workspaces. |

The Debug build of `AstraHive.AppHost` creates the dev root CA and the Global Administrator's certificate for `admin@astrahive.local` (`tools/AH.CertTool dev`, installed into `CurrentUser\My`). Sign in at https://localhost:7093 with "Sign in with a certificate".

### Production bootstrap

1. Create the root CA and the Global Administrator's certificate (once; the tool refuses to overwrite):

   ```bash
   dotnet run --project tools/AH.CertTool -- init --out <empty dir> --email <admin email> --name "<admin name>"
   ```

2. Fill the AppHost deployment parameters it prints: `certificate-root` (root.pem), `certificate-root-key` (root.key.pem, secret), `global-admin-email`, `global-admin-name`, `global-admin-certificate` (`<email>.pem`). Keep `root.key.pem` offline otherwise.
3. Hand `<email>.pfx` and its printed password to the Global Administrator. On first start IdentityService creates the Global Administrator and the main workspace "AstraHive"; the administrator then adds members and issues their certificates from the workspace members page.

### Frontend

```bash
cd ah.webapp
npm install
npm run build:watch  # build into AH.WebApi/wwwroot on every change (served at https://localhost:7093)
npm run lint         # oxlint + eslint
```

### Build & Test

```bash
dotnet build AstraHive.slnx
dotnet test AstraHive.slnx
dotnet format AstraHive.slnx --verify-no-changes
```

### EF Core Migrations

```bash
dotnet ef migrations add <Name> --project AH.Infrastructure --startup-project AH.IdentityService --output-dir Persistence/Migrations
dotnet ef database update --project AH.Infrastructure --startup-project AH.IdentityService
```

## Current State

Identity and the web console run. Virtual machine orchestration does not. Details and diagrams are in [architecture.md](architecture.md).

- Users live in PostgreSQL. `AH.IdentityService` owns the schema and applies migrations on startup in Development. `AH.WebApi` signs users in with a client certificate, or with Google/Yandex when `ah.external_login` is on, and calls that service over gRPC (see [architecture.md](architecture.md#sign-in-rules)).
- The Global Administrator (created at deployment, certificate-only) owns the main workspace, adds members by email as Administrator or Viewer, and issues, downloads and revokes their client certificates; PFX files are kept in MinIO (see [architecture.md](architecture.md#global-administrator)). With `ah.registration_enabled` on, a first Google/Yandex sign-in creates a workspace with the user as Administrator (see [architecture.md](architecture.md#workspaces)).
- `ah.webapp` is the Vue console (sign-in, welcome, profile, workspace members). Under `aspire run` it rebuilds into `AH.WebApi/wwwroot`.
- `AH.SiloHost` and the grain projects are placeholders. Orleans is not hosted yet.

Coding conventions for contributors and AI agents are documented in [`AGENTS.md`](AGENTS.md).
