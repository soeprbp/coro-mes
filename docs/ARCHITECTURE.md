# CoroMES Architecture

## Overview

CoroMES is structured like a Clean Architecture solution, but the implementation is still in a prototype-heavy stage. The boundaries are useful and mostly in place, though much of the live behavior still runs through a single Minimal API host.

## What Exists Today

### Active runtime

- `CoroMES.Api` is the primary runnable application
- `CoroMES.Infrastructure` contains the EF Core context, repository implementations, and i3X-backed repository adapters
- `CoroMES.Core` contains the real domain entities, enums, and repository interfaces
- `integration/CoroMES.Integration.Cti` is an active ingestion framework for legacy CTI/EPS files
- `industrial/CoroMES.Industrial.i3X` is an active i3X client, model, and translation layer
- `web/admin` and `web/displays` are static HTML/JS prototype UIs served by the API host

### Planned or mostly skeletal areas

- `CoroMES.Application` exists but does not yet contain the full application-service or CQRS layer described in earlier docs
- `CoroMES.Reporting` exists as a project boundary, but reporting endpoints are not implemented
- most `modules/*` projects are structural boundaries rather than code-heavy module implementations
- `Integration.TrueCommerce`, `Integration.Upkeep`, `Integration.IIoT`, `Industrial.Mqtt`, `Industrial.OpcUa`, and `Industrial.EthernetIp` are mostly placeholders or future boundaries

## Current High-Level Shape

```text
Clients
  |- Static admin UI
  |- Static display UI
  |- External callers
  |
  v
CoroMES.Api (Minimal API host)
  |- EF Core repository mode
  |- optional i3X repository mode
  |- static file hosting for /admin and /displays
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
  - static file hosting

Today, many responsibilities that would eventually move into an application layer still live here.

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

The current UI is not Blazor yet.

- `web/admin/index.html` is a static admin prototype
- `web/displays/viewer.html` is a static display viewer
- `web/displays/builder.html` is a static display configuration prototype

These pages are useful for proving workflows, but they are not yet integrated into a component-based .NET UI architecture.

## Key Gaps Between Structure and Reality

The repo was laid out for a larger future platform, but only part of that platform is implemented today.

Notable gaps:

- no reporting API implementation
- no industrial protocol API implementation
- no Blazor or other first-class .NET front end
- no completed application-service or CQRS layer
- placeholder Upkeep behavior in the API
- incomplete integration tests

## Near-Term Architectural Direction

The cleanest next steps are:

1. keep `CoroMES.Api` as the current host
2. turn `CoroMES.Application` into a real service/use-case layer
3. make CTI ingestion runnable as an actual hosted connector workflow
4. decide whether the primary front end becomes Blazor
5. either implement or trim the planned reporting and industrial surfaces

## Deployment Notes

Current local development is centered on running the Minimal API directly and optionally bringing up Docker-backed infrastructure for PostgreSQL and MQTT.

The repo is not yet in a state where the documented "full platform" deployment shape exists end to end.
