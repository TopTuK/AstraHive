# AstraHive

**A virtual machine orchestration system** built on .NET 10, Microsoft Orleans, and .NET Aspire.

AstraHive lets operators manage fleets of virtual machines grouped into clusters: connect to any VM over SSH to perform maintenance operations, and schedule recurrent tasks that run automatically across the fleet.

## Core Capabilities

- **Virtual machine management** — a registry of VMs with their connection details and state, each processed by a dedicated Orleans grain for single-threaded, consistent handling.
- **SSH maintenance operations** — users connect to a VM via SSH from a central place and run maintenance operations without juggling terminals or credentials.
- **Clusters** — VMs are grouped into clusters so operations can target a single machine or an entire group.
- **Recurrent scheduled tasks** — users schedule maintenance tasks (e.g., nightly cleanups, patch checks) that run on a recurring schedule against a VM or cluster, backed by Orleans timers/reminders.

> **Status:** early skeleton. The solution layout and tooling are in place, but the domain features above are not implemented yet. See [Current State](#current-state).

## Architecture

AstraHive follows **Clean Architecture** and is orchestrated with **.NET Aspire**:

```
┌─────────────┐     HTTP/JSON      ┌──────────────┐
│  ah.webapp  │ ─────────────────► │  AH.WebApi   │────┐
│  (Vue 3)    │                    │ (ASP.NET API)│    │ Orleans client
└─────────────┘                    └──────────────┘    │
                                          │            ▼
                                          │   ┌──────────────────┐
   ┌──────────────────┐   gRPC           │   │  AH.SiloHost     │
   │ AH.IdentityService│ ◄───────────────┼──►│  (Orleans silo)   │
   └──────────────────┘                  │   └──────────────────┘
                                          │            │
                                          ▼            ▼
                                  ┌─────────────────────────┐
                                  │ PostgreSQL (EF Core)    │
                                  └─────────────────────────┘
                     Everything provisioned & wired by Aspire
```

Key ideas:

- **Orleans grains** give each entity (a VM, a cluster, a scheduled task) a single-threaded address. Grains are thin; business rules live in the domain and application layers, and domain data persists in PostgreSQL via EF Core — not in `IPersistentState<T>`.
- **Aspire** orchestrates all resources (database, services, frontend) with service discovery — no hardcoded connection strings or URLs.
- **Layered dependencies** are strictly enforced: `AppDomain` → nothing; `Application` → `AppDomain`; `Infrastructure` → `Application`; composition roots (`WebApi`, `SiloHost`) wire everything together.

## Solution Layout

| Project | Purpose |
| --- | --- |
| `AH.AppDomain/` | Domain: entities, value objects, enums, domain errors. No dependencies. |
| `AH.Application/` | Use cases (commands/queries + handlers), DTOs, validators, `IAppDbContext`. |
| `AH.Infrastructure/` | EF Core (`AppDbContext`, configurations, migrations), external adapters. |
| `AH.WebApi/` | ASP.NET Core Web API (controllers); composition root; Orleans client. |
| `AH.Grains.Abstractions/` | Orleans grain interfaces + serializable grain messages. |
| `AH.Grains/` | Grain implementations (thin; delegate to domain/application). |
| `AH.SiloHost/` | Orleans silo host with health endpoints; composition root. |
| `AH.IdentityService/` | gRPC identity/auth service (design TBD). |
| `ah.webapp/` | Vue 3 + Vite frontend. |
| `AstraHive.AppHost/` | Aspire AppHost: orchestrates all resources. |
| `AstraHive.ServiceDefaults/` | OpenTelemetry, health checks, service discovery, HTTP resilience. |

Projects sit at the repository root; `/src/` and `/tests/` in `AstraHive.slnx` are solution folders only.

## Tech Stack

- **Backend:** .NET 10 / C# 14, ASP.NET Core controllers, Microsoft Orleans
- **Database:** PostgreSQL via EF Core + Npgsql (Aspire integration)
- **Caching:** HybridCache (in-memory L1; Redis L2 can be added via Aspire later)
- **Validation & errors:** FluentValidation, ProblemDetails (RFC 9457)
- **Identity:** gRPC service (`AH.IdentityService`), design TBD
- **Observability:** OpenTelemetry → Aspire dashboard
- **Frontend:** Vue 3, Vite 8, Node `^22.18 || >=24.12`
- **Testing:** xUnit v3, WebApplicationFactory, Testcontainers PostgreSQL, Verify

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (10.0.4xx)
- [Aspire CLI](https://learn.microsoft.com/dotnet/aspire/fundamentals/setup-tooling) (`dotnet tool install -g aspire.cli`)
- Node.js `^22.18 || >=24.12` (for the frontend)
- Docker (for PostgreSQL via Aspire, once wired)

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
npm run dev      # dev server (port 57976)
npm run lint     # oxlint + eslint
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

The solution is an early skeleton:

- The Clean Architecture layer projects exist but are **empty and not yet wired together** — no domain entities, handlers, grains, or controllers yet.
- `AH.SiloHost`, `AH.WebApi`, and `AH.IdentityService` run stock templates (Orleans and PostgreSQL wiring are planned, not installed).
- `ah.webapp` is a fresh Vue 3 scaffold.
- The Aspire AppHost currently starts the three backend services; database and frontend resources will be added as features land.

Architecture and coding conventions for contributors and AI agents are documented in [`AGENTS.md`](AGENTS.md).
