# CoroMES - Manufacturing Execution System

## Overview

CoroMES is a homebrew Manufacturing Execution System (MES) designed for manufacturing operations management. It provides real-time visibility and control over production processes, integrates with EDI systems, CMMS platforms, and industrial protocols.

## Architecture

CoroMES uses **Clean Architecture** with a modular structure:

```
src/
├── CoroMES.Api           # REST API (Minimal API)
├── CoroMES.Core          # Domain entities, interfaces
├── CoroMES.Application   # Use cases, DTOs, services
├── CoroMES.Infrastructure# Database, external services
└── CoroMES.Reporting     # Reporting API for BI tools

modules/                  # MES functional domains
├── Production
├── Quality
├── Inventory
├── Equipment
└── Workforce

integration/              # External integrations
├── TrueCommerce         # EDI (X12)
├── Upkeep               # CMMS
└── IIoT                 # Industrial IoT

industrial/               # Protocol handlers
├── Mqtt
├── OpcUa
└── EthernetIp
```

## Quick Start

### Prerequisites
- .NET 10 SDK
- Docker Desktop
- PostgreSQL (or Docker)

### Run Development

```powershell
# Start PostgreSQL
docker compose -f infra/docker/docker-compose.yml up -d

# Build solution
dotnet build

# Run API
dotnet run --project src/CoroMES.Api
```

### Swagger UI
Visit: http://localhost:5000/swagger

## Configuration

See `config/settings.yaml` for all configuration options.

## Documentation

- [Architecture](./ARCHITECTURE.md)
- [API Documentation](./API.md)
- [Reporting API](./REPORTING_API.md)
- [Integrations](./INTEGRATIONS.md)
- [Industrial Protocols](./INDUSTRIAL_PROTOCOLS.md)
- [Development Guide](./DEVELOPMENT.md)

## Version

Current: **0.1.0-alpha**

See [CHANGELOG](./CHANGELOG.md) for version history.