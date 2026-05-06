# CoroMES Architecture

## Overview

CoroMES follows **Clean Architecture** principles with a modular domain structure designed for Manufacturing Execution Systems (MES).

## High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────────────┐
│                              CLIENTS                                      │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌───────────────┐  │
│  │  Shop Floor │  │  Reporting  │  │   External  │  │   Industrial  │
│  │  Operators  │  │  (Power BI)  │  │   Systems   │  │   Devices     │
│  └──────┬──────┘  └──────┬──────┘  └──────┬──────┘  └───────┬───────┘  │
│         │                │                │                  │           │
│         ▼                ▼                ▼                  ▼           │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  ┌────────────┐  │
│  │  Core API   │  │  Reporting   │  │ Integration  │  │ Industrial │  │
│  │  /api/v1   │  │  /reporting  │  │    APIs      │  │  Protocols │  │
│  └──────────────┘  └──────────────┘  └──────────────┘  └────────────┘  │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                         APPLICATION LAYER                                │
│  ┌──────────────────────────────────────────────────────────────────────┐ │
│  │                    CoroMES.Application                               │ │
│  │         Use Cases, DTOs, Services, Mappings                         │ │
│  └──────────────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                          DOMAIN LAYER                                    │
│  ┌──────────────────────────────────────────────────────────────────────┐ │
│  │                      CoroMES.Core                                     │ │
│  │    Entities, Enums, Interfaces, Base Classes, Events               │ │
│  └──────────────────────────────────────────────────────────────────────┘ │
│         │                     │                    │                     │
│         ▼                     ▼                    ▼                     │
│  ┌─────────────┐     ┌─────────────┐     ┌─────────────┐                 │
│  │  Production │     │   Quality   │     │   Inventory│                 │
│  │   Module    │     │   Module    │     │   Module   │                 │
│  └─────────────┘     └─────────────┘     └─────────────┘                 │
│         │                     │                    │                     │
│         ▼                     ▼                    ▼                     │
│  ┌─────────────┐     ┌─────────────┐     ┌─────────────┐                 │
│  │  Equipment  │     │  Workforce  │     │ Integration │                 │
│  │   Module    │     │   Module    │     │   Modules   │                 │
│  └─────────────┘     └─────────────┘     └─────────────┘                 │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                      INFRASTRUCTURE LAYER                                │
│  ┌──────────────────────────────────────────────────────────────────────┐ │
│  │                   CoroMES.Infrastructure                              │ │
│  │     EF Core, PostgreSQL, External API Clients, File Handlers        │ │
│  └──────────────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                           DATA LAYER                                     │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌───────────────┐  │
│  │ PostgreSQL  │  │   MQTT      │  │   OPC-UA    │  │   Ethernet/IP │  │
│  │  Database   │  │   Broker    │  │   Server    │  │    Devices    │  │
│  └─────────────┘  └─────────────┘  └─────────────┘  └───────────────┘  │
└─────────────────────────────────────────────────────────────────────────┘
```

## Layer Responsibilities

### API Layer
- **CoroMES.Api**: Minimal API endpoints, request/response handling
- **CoroMES.Reporting**: Aggregated data endpoints for BI tools

### Application Layer
- **CoroMES.Application**: Business logic orchestration
  - Use cases (CQRS pattern)
  - DTOs (Data Transfer Objects)
  - Service interfaces
  - Mapping profiles

### Domain Layer
- **CoroMES.Core**: Domain model
  - Entities (WorkOrder, Equipment, Material, Operator)
  - Value objects
  - Enums (WorkOrderStatus, EquipmentStatus)
  - Interfaces (IRepository, IService)
  - Domain events

### Modules
- **Production**: Work orders, operations, scheduling
- **Quality**: Inspections, NCR, traceability
- **Inventory**: Materials, BOM, movements
- **Equipment**: Machines, maintenance, status
- **Workforce**: Shifts, labor, operators

### Integration Layer
- **TrueCommerce**: EDI parsing (X12 850/810)
- **Upkeep**: CMMS API integration
- **IIoT**: Industrial IoT gateway

### Infrastructure Layer
- **CoroMES.Infrastructure**: External concerns
  - Entity Framework Core
  - PostgreSQL provider
  - HTTP clients for external APIs
  - File system handlers

### Industrial Layer
- **Mqtt**: MQTTnet client/broker
- **OpcUa**: OPC Foundation SDK
- **EthernetIp**: Ethernet/IP for AB PLCs

## Design Patterns

| Pattern | Usage |
|---------|-------|
| **Repository** | Data access abstraction |
| **Unit of Work** | Transaction management |
| **CQRS** | Command/Query separation in Application layer |
| **Mediator** | Request/handler pipeline |
| **Factory** | Entity creation |
| **Dependency Injection** | All layers use DI |

## API Structure

### Core API (`/api/v1`)
```
/api/v1/workorders
/api/v1/workorders/{id}
/api/v1/equipment
/api/v1/inventory
/api/v1/quality
/api/v1/workforce
```

### Reporting API (`/reporting/v1`)
```
/reporting/v1/oee
/reporting/v1/production/daily
/reporting/v1/production/by-line
/reporting/v1/quality/inspections
/reporting/v1/equipment/downtime
/reporting/v1/inventory/current
/reporting/v1/labor/hours
```

### Industrial API (`/industrial/v1`)
```
/industrial/v1/mqtt/publish
/industrial/v1/mqtt/subscribe
/industrial/v1/opcua/browse
/industrial/v1/opcua/read
/industrial/v1/opcua/write
/industrial/v1/ethernetip/read
/industrial/v1/ethernetip/write
```

## Data Flow

```
User Action → API Controller → Use Case → Repository → Database
                    ↓
              Domain Entity
                    ↓
            Domain Event (optional)
                    ↓
           Integration Handlers
                    ↓
         External Systems (EDI, CMMS)
```

## Configuration

Configuration is managed through `config/settings.yaml` with support for:
- Database connection
- API settings
- Integration credentials
- Industrial protocol settings

Environment variables override YAML config (for secrets):
- `UPKEEP_API_KEY`
- `TRUECOMMERCE_PASSWORD`
- etc.

## Deployment Options

1. **Docker Compose**: PostgreSQL + MQTT + Application
2. **Kubernetes**: Microservices or monolith
3. **Traditional**: IIS, Kestrel directly

See [DEPLOYMENT.md](./DEVELOPMENT.md) for details.