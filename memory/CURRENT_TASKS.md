# Current Tasks

**Last Updated:** 2026-05-28

## Active Work

### Phase 1: Foundation Setup (In Progress)

- [ ] Design MCP server surface for AI agents
  - [ ] Define read-only resources/tools for work orders, equipment, materials, production events, quality records, reports, and i3X data
  - [ ] Define authentication, authorization, plant/role scoping, audit logging, and rate limiting requirements
  - [ ] Identify which write-capable tools require human approval or extra safeguards

- [ ] Design CTI-to-CoroMES connector program
  - [x] Capture initial Welch CTI/EPS integration facts and public CTI/ePS product research in `docs/CTI_RESEARCH_BRIEF.md`
  - [x] Create reusable Codex skill `$welch-cti-connector` for future CTI/EPS connector work
  - [x] Back up `$welch-cti-connector` in the repo under `docs/skills/welch-cti-connector`
  - [x] Start .NET CTI connector framework in `integration/CoroMES.Integration.Cti`
  - [ ] Gather CTI data source details, access method, schema/files/API, refresh cadence, and ownership boundaries
  - [ ] Define ingestion, mapping, reconciliation, sync status, and error handling flows
  - [ ] Keep CTI-specific logic isolated from core CoroMES modules

- [ ] Design modular machine/MES-to-i3X translator architecture
  - [ ] Define shared translator abstraction for source discovery, object mapping, relationships, values, history, and writes
  - [ ] Plan translator modules for CTI, MQTT, OPC-UA, Ethernet/IP, and future machine/MES sources
  - [ ] Ensure translator outputs align with i3X and MCP exposure requirements

- [ ] Add project references
  - [ ] Modules → Core
  - [ ] Application → Core
  - [ ] Infrastructure → Core
  - [ ] Api → Application, Infrastructure
  - [ ] Reporting → Core, Infrastructure
  - [ ] Integrations → Core

- [ ] Add NuGet packages
  - [ ] EF Core + PostgreSQL
  - [ ] MQTTnet
  - [ ] OPC-UA SDK
  - [ ] Ethernet/IP library

- [ ] Configure Docker Compose
  - [ ] PostgreSQL container
  - [ ] Mosquitto MQTT broker

- [ ] Create Core domain entities
  - [ ] WorkOrder
  - [ ] Equipment
  - [ ] Material
  - [ ] Operator

- [ ] Create API endpoints

## Blockers

None yet.

## Next Actions

1. Use `docs/CTI_RESEARCH_BRIEF.md` when designing the CTI connector and discovery scripts
2. Export CTI01/svwpcti01 services, Amtech task server jobs, and staging share folder listings
3. Collect representative `.dat` / `.cov` sample sets for normal, exception/rework, and canceled/voided scenarios
4. Register real CTI file layouts in the parser once sample files are available
5. Add project references to establish Clean Architecture
6. Add required NuGet packages
7. Set up Docker Compose
