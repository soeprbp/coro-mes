# CoroMES Project State

**Last Updated:** 2026-04-16

## Overall Status

- **Phase:** Initial Setup (In Progress)
- **Current Version:** 0.1.0-alpha
- **Framework:** .NET 10
- **Database:** PostgreSQL (via Docker)

## Solution Structure

- **Solution File:** `CoroMES.sln`
- **Total Projects:** 19 projects

### Core Layer (src/)
| Project | Purpose |
|---------|---------|
| CoroMES.Api | REST API (Minimal API) |
| CoroMES.Core | Domain entities, interfaces, enums |
| CoroMES.Application | Use cases, DTOs, services |
| CoroMES.Infrastructure | EF Core, PostgreSQL, external services |
| CoroMES.Reporting | Reporting API for BI tools |

### Modules Layer (modules/)
| Project | Purpose |
|---------|---------|
| CoroMES.Production | Work orders, operations, tracking |
| CoroMES.Quality | Inspections, NCR, traceability |
| CoroMES.Inventory | Materials, BOM, movements |
| CoroMES.Equipment | Machines, maintenance, status |
| CoroMES.Workforce | Shifts, labor, operators |

### Integration Layer (integration/)
| Project | Purpose |
|---------|---------|
| CoroMES.Integration.TrueCommerce | EDI (X12 850/810) |
| CoroMES.Integration.Upkeep | CMMS API |
| CoroMES.Integration.IIoT | Industrial IoT gateway |

### Industrial Layer (industrial/)
| Project | Protocol |
|---------|----------|
| CoroMES.Industrial.Mqtt | MQTT |
| CoroMES.Industrial.OpcUa | OPC-UA |
| CoroMES.Industrial.EthernetIp | Ethernet/IP |

### Tests
- CoroMES.UnitTests
- CoroMES.IntegrationTests

## Decisions Made

1. **Solution Name:** CoroMES (homebrew MES)
2. **Framework:** .NET 10 Minimal API
3. **Database:** PostgreSQL
4. **Architecture:** Clean Architecture + Modular
5. **API Strategy:** Separate Core API + Reporting API
6. **Industrial Protocols:** MQTT, OPC-UA, Ethernet/IP (out of box)
7. **External Integrations:** TrueCommerce (EDI), Upkeep.com (CMMS)

## What's Been Done

- [x] Created folder structure
- [x] Created .NET solution (19 projects)
- [x] Added project references (Clean Architecture)
- [x] Added NuGet packages (EF Core, PostgreSQL, MQTTnet, MediatR, Swagger)
- [x] Created Core entities (WorkOrder, Equipment, Material, Operator, Quality)
- [x] Created ApplicationDbContext with EF Core
- [x] Created API endpoints (WorkOrders, Equipment, Materials, Operators, Quality, Upkeep, Displays)
- [x] Added Equipment fields: PartsPerMinute, SqFtPerDay, CycleTimeSeconds, Protocol, UpkeepAssetId
- [x] Created Docker support (Dockerfile, docker-compose.yml)
- [x] Created Admin web interface (web/admin/index.html)
- [x] Created Shop Floor Displays (web/displays/viewer.html)
- [x] Created Display Builder (web/displays/builder.html)
- [x] Created documentation

## Admin Interface Features

- Password-protected access
- Equipment management (add, edit, delete)
- Auto-detect protocol (MQTT, OPC-UA, Ethernet/IP)
- Production capabilities (PPM, Sq Ft/Day, Cycle Time)
- Upkeep asset linking
- Protocol configuration

## Shop Floor Display Features

- 16:9 aspect ratio displays
- Auto-refreshing (configurable interval)
- Multiple display types: OEE, Production, Quality, Equipment
- Assign equipment to displays
- Fullscreen mode (click or press F)
- Real-time data visualization

## Next Steps

1. Run `docker compose up` to start full stack
2. Create EF Core migrations
3. Add auto-detect protocol service implementation

## Key References

- **EDI:** TrueCommerce (existing setup, connects via API/inbound files)
- **CMMS:** Upkeep.com API
- **Industrial:** MQTT (sensors), OPC-UA (SCADA/Siemens), Ethernet/IP (Allen-Bradley)

## Documentation

- Main docs: `./docs/`
- Memory: `./memory/`
- Changelog: `./changelog/`