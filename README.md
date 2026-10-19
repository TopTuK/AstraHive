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
| `AH.IdentityService/` | gRPC service that owns users. See [architecture.md](architecture.md). |
| `ah.webapp/` | Vue 3 + Vite frontend. |
| `AstraHive.AppHost/` | Aspire AppHost: orchestrates all resources. |
| `AstraHive.ServiceDefaults/` | OpenTelemetry, health checks, service discovery, HTTP resilience. |

Projects sit at the repository root; `/src/` and `/tests/` in `AstraHive.slnx` are solution folders only.

## Tech Stack

- **Backend:** .NET 10 / C# 14, ASP.NET Core controllers, Microsoft Orleans
- **Database:** PostgreSQL via EF Core + Npgsql (Aspire integration)
- **Caching:** HybridCache (in-memory L1; Redis L2 can be added via Aspire later)
- **Validation & errors:** FluentValidation, ProblemDetails (RFC 9457)
- **Identity:** Google and Yandex sign-in in `AH.WebApi`; users owned by gRPC `AH.IdentityService`
- **Observability:** OpenTelemetry → Aspire dashboard
- **Frontend:** Vue 3, Vite 8, Node `^22.18 || >=24.12`
- **Testing:** xUnit v3, WebApplicationFactory, Testcontainers PostgreSQL, Verify

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (10.0.4xx)
- [Aspire CLI](https://learn.microsoft.com/dotnet/aspire/fundamentals/setup-tooling) (`dotnet tool install -g aspire.cli`)
- Node.js `^22.18 || >=24.12` (for the frontend)
- Docker (PostgreSQL via Aspire)

### Run

```bash
# Everything: Aspire dashboard + all services
aspire run
# or
dotnet run --project AstraHive.AppHost
```

The Aspire dashboard shows every resource's logs, traces, and metrics.

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
dotnet ef migrations add <Name> --project AH.Infrastructure --startup-project AH.WebApi --output-dir Persistence/Migrations
dotnet ef database update --project AH.Infrastructure --startup-project AH.WebApi
```

## Current State

Identity and the web console run. Virtual machine orchestration does not. Details and diagrams are in [architecture.md](architecture.md).

- Users live in PostgreSQL. `AH.IdentityService` owns the schema and applies migrations on startup in Development. `AH.WebApi` signs users in with Google or Yandex and calls that service over gRPC.
- `ah.webapp` is the Vue console (sign-in, welcome, profile). Under `aspire run` it rebuilds into `AH.WebApi/wwwroot`.
- `AH.SiloHost` and the grain projects are placeholders. Orleans is not hosted yet.

Coding conventions for contributors and AI agents are documented in [`AGENTS.md`](AGENTS.md).
