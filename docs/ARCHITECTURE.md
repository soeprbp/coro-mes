# CoroMES Architecture

## Overview

CoroMES is structured like a Clean Architecture solution, but the implementation is still in a prototype-heavy stage. In the Blazor fork, `CoroMES.Web` is the forward user-facing host while `CoroMES.Api` remains the backend Minimal API host and API route source of truth.

## What Exists Today

### Active runtime

- `CoroMES.Web` is the forward Blazor host for admin and shop-floor display workflows
- `CoroMES.Api` is the backend Minimal API host
- `CoroMES.Infrastructure` contains the EF Core context, repository implementations, and i3X-backed repository adapters
- `CoroMES.Core` contains the real domain entities, enums, and repository interfaces
- `integration/CoroMES.Integration.Cti` is an active ingestion framework for legacy CTI/EPS files
- `industrial/CoroMES.Industrial.i3X` is an active i3X client, model, and translation layer
- old static URLs under `web/admin` and `web/displays` are migration compatibility paths that should redirect to Blazor routes or remain shimmed until replaced

### Planned or mostly skeletal areas

- `CoroMES.Application` exists but does not yet contain the full application-service or CQRS layer described in earlier docs
- `CoroMES.Reporting` exists as a project boundary, but reporting endpoints are not implemented
- most `modules/*` projects are structural boundaries rather than code-heavy module implementations
- `Integration.TrueCommerce`, `Integration.Upkeep`, `Integration.IIoT`, `Industrial.Mqtt`, `Industrial.OpcUa`, and `Industrial.EthernetIp` are mostly placeholders or future boundaries

## Current High-Level Shape

```text
Clients
  |- Blazor admin UI
  |- Blazor display UI
  |- External callers
  |
  v
CoroMES.Web (forward Blazor host)
  |
  v
CoroMES.Api (backend Minimal API host)
  |- EF Core repository mode
  |- optional i3X repository mode
  |- API route parity for /api/v1, /health, and display config endpoints
  |- compatibility redirects for old static URLs
  |
  v
CoroMES.Core + CoroMES.Infrastructure
  |- domain entities
  |- repository interfaces
  |- ApplicationDbContext
  |- EF repositories
  |- i3X repository adapters
  |
  +--> CTI ingestion framework
  +--> future external integrations
  +--> future industrial protocol services
```

## Layer Responsibilities

### API Layer

- **`CoroMES.Api`**
  - application startup
  - database provider selection
  - repository registration
  - HTTP endpoints
  - compatibility redirects or static shims during migration

Today, many responsibilities that would eventually move into an application layer still live here.

### Web Layer

- **`CoroMES.Web`**
  - forward Blazor host
  - admin workflow pages
  - shop-floor display viewer and builder pages
  - client access to the existing backend API route contract

### Domain Layer

- **`CoroMES.Core`**
  - base entities
  - production, equipment, inventory, workforce, and quality entities
  - enums
  - repository interfaces

This is the most complete and stable architectural layer in the repo.

### Infrastructure Layer

- **`CoroMES.Infrastructure`**
  - EF Core `ApplicationDbContext`
  - migrations
  - generic and specialized repositories
  - i3X-backed repository adapters

Infrastructure currently carries a large share of the practical behavior.

### CTI Migration Layer

- **`CoroMES.Integration.Cti`**
  - file discovery
  - raw file capture
  - conservative parsing
  - validation
  - quarantine handling

This connector is intentionally file-first and read-only toward CTI/Amtech production systems.

### i3X Layer

- **`CoroMES.Industrial.i3X`**
  - HTTP client for i3X servers
  - object and value models
  - translator between CoroMES entities and i3X semantics

This is one of the more advanced parts of the repository, though some mappings still reuse object types and need refinement.

## Current Data Modes

### Default local mode

- SQLite
- configured in `src/CoroMES.Api/appsettings.json`
- used for local development and quick startup

### Production-intended mode

- PostgreSQL
- supported through EF Core and Docker configuration
- checked-in migrations target PostgreSQL

### Optional external data-fabric mode

- i3X
- enabled via configuration
- swaps EF-backed repositories for i3X-backed repositories

## UI Architecture Today

The Blazor fork moves the primary UI direction to `src/CoroMES.Web`.

- `CoroMES.Web` is the forward host for admin and display workflows.
- `web/admin/index.html`, `web/displays/viewer.html`, and `web/displays/builder.html` are pre-Blazor prototype paths.
- Old static URLs should redirect to Blazor routes or remain available as compatibility shims until their replacement screens are complete.

The pre-Blazor state is preserved at branch/tag `pre-blazor-2026-06-10` and in `C:\Users\soperbp\OneDrive - Welch Packaging Group\Scripts\workdev\CoroMES-source-backup-2026-06-10.zip`.

## Key Gaps Between Structure and Reality

The repo was laid out for a larger future platform, but only part of that platform is implemented today.

Notable gaps:

- no reporting API implementation
- no industrial protocol API implementation
- Blazor screens are newly established as the forward UI path and still need build-out
- no completed application-service or CQRS layer
- placeholder Upkeep behavior in the API
- incomplete integration tests

## Near-Term Architectural Direction

The cleanest next steps are:

1. build `CoroMES.Web` as the forward Blazor host
2. turn `CoroMES.Application` into a real service/use-case layer
3. make CTI ingestion runnable as an actual hosted connector workflow
4. preserve API route parity while replacing static URLs with redirects
5. either implement or trim the planned reporting and industrial surfaces

## Deployment Notes

Current local development is centered on running the Minimal API directly and optionally bringing up Docker-backed infrastructure for PostgreSQL and MQTT.

The repo is not yet in a state where the documented "full platform" deployment shape exists end to end.
