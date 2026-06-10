# CoroMES Project State

**Last Updated:** 2026-06-10

## Overall Status

- **Phase:** Prototype foundation complete; integration-driven modernization work in progress
- **Current Version:** 0.1.0-alpha
- **Framework:** .NET 10
- **Forward UI Host:** `src/CoroMES.Web`
- **Backend API Host:** `src/CoroMES.Api`
- **Default Local Database:** SQLite
- **Production-Intended Database:** PostgreSQL

## Executive Summary

CoroMES is no longer just a scaffold. The repository now contains:

- a working Minimal API host
- domain entities and repository interfaces
- EF Core persistence and migrations
- a forward Blazor host for admin and shop-floor display workflows
- static admin and shop-floor display prototype URLs that need redirects or compatibility shims during migration
- an i3X client and repository adapter layer
- a documented plan for using `soeprbp/mes-vision` as the first vision telemetry test endpoint
- a CTI/EPS file-ingestion framework designed for safe migration work

The codebase still stops short of a full MES application. Several architectural boundaries exist as project shells or planned expansion points rather than finished product areas.

## Long-Term Direction

- Replace the legacy CTI/EPS MES and SCADA functions with CoroMES.
- Use the current MES/SCADA system as a temporary data source during migration where useful.
- Preserve corrugated-specific workflows such as scheduling visibility, roll traceability, forklift/clamp truck scanning, wet-end roll usage, butt roll handling, downtime, scrap, and ERP feedback.
- Prefer modern web or tablet UX, event-driven integration, and managed industrial connectivity.
- Keep CTI-specific migration logic isolated from the long-term CoroMES domain model.

## Solution Structure

- **Solution File:** `CoroMES.sln`
- **Total Projects:** 19

### Core Layer (`src/`)

| Project | Current Reality |
|---------|-----------------|
| CoroMES.Api | Active Minimal API host and static file host |
| CoroMES.Web | Forward Blazor host for admin and shop-floor display workflows |
| CoroMES.Core | Active domain entities, enums, interfaces |
| CoroMES.Application | Present, but still light; not yet the full service/use-case layer |
| CoroMES.Infrastructure | Active EF Core, repositories, migrations, i3X adapters |
| CoroMES.Reporting | Present as a boundary, not yet a real reporting API |

### Modules Layer (`modules/`)

These projects mainly exist as structural boundaries right now. Most business logic still lives in `Core`, `Infrastructure`, and `Api`.

### Integration Layer (`integration/`)

| Project | Current Reality |
|---------|-----------------|
| CoroMES.Integration.TrueCommerce | Planned boundary |
| CoroMES.Integration.Cti | Active framework for file discovery, raw capture, parsing, validation, quarantine |
| CoroMES.Integration.Upkeep | Planned boundary; API currently uses placeholders |
| CoroMES.Integration.IIoT | Planned boundary; likely home for the MES-Vision collector or shared telemetry ingestion patterns |

### Industrial Layer (`industrial/`)

| Project | Current Reality |
|---------|-----------------|
| CoroMES.Industrial.i3X | Active and substantive |
| CoroMES.Industrial.Mqtt | Present, limited implementation today |
| CoroMES.Industrial.OpcUa | Present, limited implementation today |
| CoroMES.Industrial.EthernetIp | Present, limited implementation today |

### Tests

- `CoroMES.UnitTests` contains real coverage for CTI ingestion and i3X client behavior
- `CoroMES.IntegrationTests` now includes the first Blazor host smoke coverage for auth gates and equipment audit logging

## What Is Working Today

- solution builds successfully
- Minimal API host runs locally
- health endpoint is available
- foundational CRUD-like endpoints exist for work orders, equipment, materials, and operators
- read endpoints exist for quality inspections and NCRs
- prototype Upkeep endpoints and display endpoints exist
- `CoroMES.Web` is the forward Blazor host
- old static admin and display URLs are expected to redirect or remain compatible during migration
- EF Core persistence is wired up
- PostgreSQL migrations are checked in
- optional i3X repository mode exists behind configuration
- MES-Vision integration requirements are documented in `docs/MES_VISION_INTEGRATION.md`
- CTI ingestion primitives are implemented and unit-tested
- first Blazor admin auth gate protects `/admin`, `/displays/builder`, and `/api/v1/*`
- first-pass audit logging records equipment create/update/delete actions
- first Blazor integration tests exercise public versus protected route behavior and equipment audit writes

## Important Limitations

- Blazor is now the forward UI path, but screens, redirect coverage, and persisted display definitions still need build-out
- no reporting API implementation despite the project and older docs
- no industrial API surface despite earlier documentation
- no persisted vision telemetry model yet despite MES-Vision being identified as the first test endpoint
- first auth gate is cookie-based and suitable for migration/local control, not final enterprise identity
- audit logging covers equipment mutations only so far
- many integrations are planned boundaries rather than complete implementations
- some prototype behaviors are still hard-coded or placeholder-based
- several memory and doc files had drifted from the code before this update

## What Was Recently Advanced

- i3X repository integration
- CTI/EPS research brief and reusable skill backup
- CTI ingestion project with file discovery, capture, parser, validator, and pipeline
- security audit documentation and helper script additions in the working tree
- Blazor fork preservation at branch/tag `pre-blazor-2026-06-10`
- source backup zip captured at `C:\Users\soperbp\OneDrive - Welch Packaging Group\Scripts\workdev\CoroMES-source-backup-2026-06-10.zip`
- Jane security pass added the first Blazor admin/API auth gate
- first audit foundation added `AuditLog` storage and equipment mutation logging
- first Blazor host integration tests started for auth and audit behavior
- MES-Vision repository inspected and documented as a vision telemetry/i3X test endpoint

## Recommended Next Steps

1. keep the Blazor auth/audit integration smoke suite green while expanding the host
2. move business behavior out of the large API `Program.cs` into endpoint modules and application services
3. build a read-only MES-Vision collector using i3X discovery, current values, and event history
4. persist display builder configurations and load them through the Blazor viewer
5. replace the temporary admin access-code gate with the chosen enterprise identity model
6. turn the CTI ingestion framework into a runnable connector workflow or host
7. collect real Welch CTI sample files and source-system inventory

## Key References

- `docs/CTI_RESEARCH_BRIEF.md`
- `docs/INTEGRATIONS.md`
- `docs/MES_VISION_INTEGRATION.md`
- `docs/API.md`
- `docs/ARCHITECTURE.md`
- `memory/LONG_TERM_GOALS.md`
- `memory/DECISIONS.md`
