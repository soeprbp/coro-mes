# CoroMES Architecture — As Implemented (Findings)

> Status: **repository-grounded review of what actually exists today**, as opposed to `ARCHITECTURE.md`, which describes the target design. Written from a direct read of the source tree.

## What it is
CoroMES is a **pre-alpha Manufacturing Execution System (MES) modernization** for Welch Packaging, aiming to replace legacy CTI/EPS MES + SCADA capabilities with browser-based, auditable, plant-aware workflows (README.md:3-5). The docs describe a *target* Clean Architecture; only a fraction is actually implemented — most module/integration projects are empty shells.

## Solution layout (22 projects, `CoroMES.slnx`)

| Group | Projects | Status |
|---|---|---|
| `src/` | `CoroMES.Api`, `CoroMES.Application`, `CoroMES.Core`, `CoroMES.Infrastructure`, `CoroMES.Reporting` | Api/Core/Infrastructure implemented; Application & Reporting are empty shells |
| `modules/` | Production, Quality, Inventory, Equipment, Workforce | **0 authored `.cs` files each** — shells only |
| `industrial/` | `CoroMES.Industrial.i3X` (implemented), EthernetIp / Mqtt / OpcUa (shells; Mqtt csproj pre-wires MQTTnet) | mostly planned |
| `integration/` | `CoroMES.Integration.Cti` (implemented), IIoT / TrueCommerce / Upkeep (shells) | mostly planned |
| `tests/` | UnitTests (2 files), IntegrationTests (empty) | minimal |

## Layer model (intended)
Per docs/ARCHITECTURE.md:15: **API → Application (CQRS/MediatR) → Domain (`CoroMES.Core`) → Infrastructure (EF Core, Postgres, external clients) → data (PostgreSQL / MQTT / OPC-UA / Ethernet-IP)**. Planned patterns: Repository, Unit of Work, CQRS, Mediator, DI. The `CoroMES.Application` shell references MediatR 12.4.1 and all modules/integrations, so it is meant to become the orchestration hub.

## What's actually implemented today

### 1. Composition root — `src/CoroMES.Api/Program.cs` (single file, Minimal API)
- Picks the DB provider from config: PostgreSQL (`UseNpgsql`, runs `MigrateAsync`) vs SQLite default (`database/coromes.db`, `EnsureCreated`) — Program.cs:12-43.
- Toggles the **repository backend** via `i3x:enabled`: registers i3X-backed repositories for all 13 repository interfaces, else the EF Core ones — Program.cs:46-84.
- Serves the static `web/` folder as web root, maps `/health` plus prototype REST endpoints under `/api/v1` (workorders, equipment, materials, operators, quality, displays, fake Upkeep stubs) — Program.cs:112-337. **No auth, no validation, EF entities exposed directly.**

### 2. Domain — `CoroMES.Core`
Entities (WorkOrder, Equipment, Material, Operator, Quality…), enums, and 13 repository interfaces (`IWorkOrderRepository`, `IEquipmentRepository`, …) under `Core/Interfaces/Repositories/`.

### 3. Infrastructure — `CoroMES.Infrastructure`
- `Data/ApplicationDbContext.cs` — 13 `DbSet`s (work orders/operations, equipment/maintenance, materials/BOM/movements, operators/shifts/labor, inspections/NCR) with fluent config for unique keys + relationships (ApplicationDbContext.cs:11-102).
- EF Core generic `Repository<T>` + 13 concrete repos; checked-in EF migration targets PostgreSQL (SQLite is dev-only, uses `EnsureCreated`).
- `Repositories/i3x/` — `I3XRepository` + adapter classes that implement the same repository interfaces **against the i3X server instead of the DB**.

### 4. i3X industrial client — `CoroMES.Industrial.i3X`
`I3XClient` (HTTP/REST client for info, objects, values, history, subscriptions, relationships — I3XClient.cs:11-33), MES object-type catalog, and `MesEntityTranslator` mapping i3X objects to domain entities. Enabled via `i3x:enabled` + `i3x:baseUrl`/`apiKey` config.

### 5. CTI file ingestion — `CoroMES.Integration.Cti`
A read-only ingestion framework — abstraction interfaces (discovery, raw store, parser, validator, pipeline), a `ConservativeCtiFileParser`, filesystem discovery/store, and quarantine of invalid files. Currently a standalone library (not wired into `Program.cs`).

### 6. Frontend
Static prototypes only — `web/admin/index.html`, `web/displays/builder.html`, `web/displays/viewer.html`.

## Deployment
Root `docker-compose.yml` runs **postgres:16-alpine + eclipse-mosquitto:2 + the API** (Dockerfile at `src/CoroMES.Api/Dockerfile`) on a shared bridge network, with i3X configuration passed as env vars (docker-compose.yml:40-62). Local dev default is plain `dotnet run --project src/CoroMES.Api` with SQLite.

## Tech stack
.NET 10 (`net10.0`), EF Core 10.0.9 (SQLite + Npgsql), MediatR 12.4.1 (planned use), MQTTnet (planned), Swashbuckle/Swagger (binaries present, not wired per README), Newtonsoft.Json.

## Key caveats
- **Target ≠ reality**: the modular domain, CQRS/event bus, reporting, industrial protocols (OPC-UA/Ethernet-IP/MQTT), and TrueCommerce/Upkeep/IIoT integrations are **planned work**, not running behavior (ARCHITECTURE.md:9).
- **Not production-safe**: no authentication/authorization; README explicitly warns not to expose it (README.md:5).
- The checked-in `changeme` DB password and `config/settings.yaml` not being loaded are dev-only concerns (ARCHITECTURE.md:195-197).
