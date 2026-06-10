# CoroMES - Manufacturing Execution System

## Overview

CoroMES is a homegrown Manufacturing Execution System (MES) for corrugated manufacturing modernization. The current codebase is an early working prototype focused on:

- core MES domain entities and repositories
- a Minimal API for foundational CRUD operations
- static admin and shop-floor display web pages
- a file-first CTI/EPS migration connector framework
- an i3X client and repository adapter layer

The long-term goal is to replace legacy CTI/EPS MES and SCADA workflows with a modern .NET platform while preserving plant-critical operational behavior.

## Current State

As of June 2026, the repository is beyond initial scaffolding but not yet a complete product surface.

- `src/CoroMES.Api` is the main runnable application
- the API supports foundational endpoints for work orders, equipment, materials, operators, quality, Upkeep placeholders, and display configs
- persistence currently defaults to SQLite for local development
- PostgreSQL remains the intended production database path
- i3X support exists behind configuration and can swap repository implementations
- CTI integration has a safe ingestion framework but is not yet a full runnable connector host
- `web/admin` and `web/displays` are plain HTML/JS prototype interfaces, not Blazor

## Solution Structure

```text
src/
├── CoroMES.Api            # Main Minimal API host
├── CoroMES.Core           # Domain entities, enums, repository interfaces
├── CoroMES.Application    # Reserved for application services/use cases
├── CoroMES.Infrastructure # EF Core, repositories, i3X adapters
└── CoroMES.Reporting      # Reserved for reporting surface

modules/
├── CoroMES.Production
├── CoroMES.Quality
├── CoroMES.Inventory
├── CoroMES.Equipment
└── CoroMES.Workforce

integration/
├── CoroMES.Integration.Cti          # Active CTI/EPS ingestion framework
├── CoroMES.Integration.TrueCommerce # Planned EDI integration boundary
├── CoroMES.Integration.Upkeep       # Planned CMMS integration boundary
└── CoroMES.Integration.IIoT         # Planned IIoT integration boundary

industrial/
├── CoroMES.Industrial.i3X       # Active i3X client, models, translators
├── CoroMES.Industrial.Mqtt
├── CoroMES.Industrial.OpcUa
└── CoroMES.Industrial.EthernetIp
```

## Quick Start

### Prerequisites

- .NET 10 SDK
- Docker Desktop

### Run locally

```powershell
# Optional infrastructure for PostgreSQL/MQTT experimentation
docker compose -f infra/docker/docker-compose.yml up -d

# Build
dotnet build CoroMES.sln

# Run the API host
dotnet run --project src/CoroMES.Api
```

### Default local runtime

By default, the app uses the SQLite connection string in `src/CoroMES.Api/appsettings.json`.

- Health check: `http://localhost:5000/health`
- Admin UI: `http://localhost:5000/admin`
- Display viewer: `http://localhost:5000/displays/viewer.html`
- Display builder: `http://localhost:5000/displays/builder.html`

## Testing

```powershell
dotnet test CoroMES.sln
```

Unit tests currently cover the CTI ingestion framework and i3X client behavior.

## Documentation

- [Documentation Index](./docs/INDEX.md)
- [Architecture](./docs/ARCHITECTURE.md)
- [API](./docs/API.md)
- [Integrations](./docs/INTEGRATIONS.md)
- [Industrial Protocols](./docs/INDUSTRIAL_PROTOCOLS.md)
- [Development Guide](./docs/DEVELOPMENT.md)
- [Project State](./memory/PROJECT_STATE.md)
- [Current Tasks](./memory/CURRENT_TASKS.md)

## Version

Current: **0.1.0-alpha**

See [CHANGELOG.md](./CHANGELOG.md) for version history.
