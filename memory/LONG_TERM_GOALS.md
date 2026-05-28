# Long-Term Goals

**Last Updated:** 2026-05-26

## Strategic Direction

CoroMES is intended to replace the MES and SCADA functions currently handled by the legacy CTI/EPS ecosystem. The first modernization priority is not a full ERP replacement; it is to move plant-floor execution, production visibility, roll inventory workflows, machine connectivity, and operator/forklift interfaces into the modern CoroMES platform.

During the transition, the current MES/SCADA system may be used as a temporary data source. This allows CoroMES to be introduced incrementally while preserving continuity for orders, schedules, roll inventory, machine signals, and production history.

## Legacy System Context

The current legacy corrugated MES environment includes:

- Production scheduling for corrugator lineups and converting machine schedules.
- Plant-floor execution and data collection for job completion, scrap, downtime, and ERP feedback.
- Roll stock inventory and material handling from receiving through consumption.
- Forklift/clamp truck tablet workflows for scanning and delivering rolls just in time.
- Wet-end interfaces for roll usage, leftover butt rolls, and corrugator activity.
- Machine integration through legacy drivers, workstation configuration, and custom config files.
- ERP integration through bridging software, intermediate data stores, polling, and file/database exchange.

## Modernization Goals

1. Replace CTI/EPS MES capabilities with CoroMES modules for production, inventory, quality, equipment, workforce, and reporting.
2. Replace legacy SCADA-style machine connectivity with modern industrial integrations using MQTT, OPC-UA, and Ethernet/IP.
3. Preserve critical corrugated workflows: roll traceability, clamp truck delivery, corrugator wet-end tracking, scrap/downtime capture, job completion, and schedule visibility.
4. Use the current system as a migration data source where useful, but avoid making it a permanent architectural dependency.
5. Move from batch/polling integration toward event-driven and API-based communication with ERP and plant-floor systems.
6. Provide web-based scheduler, operator, supervisor, and forklift interfaces that work well on tablets and shop-floor devices.
7. Support multi-plant operation through centralized configuration, plant-aware services, and shared deployment patterns.
8. Improve uptime and maintainability by eliminating thick-client workstation setup, Citrix-only floor interfaces, local driver files, and brittle intermediate databases.

## Target Architecture Themes

- Modular services aligned to MES domains: scheduling, work orders, floor execution, roll inventory, quality, equipment, workforce, reporting, and integration.
- Event bus for operational events such as order release, schedule publish, roll movement, roll consumption, job start, job completion, downtime, scrap, and ERP posting.
- API gateway for web and mobile applications.
- MCP server for AI agents to browse, query, and safely act on MES context through governed tools/resources.
- Edge gateway for plant equipment and scanner integration.
- PostgreSQL-backed operational data with append-only/auditable movement and production history.
- ERP integration service that isolates Amtech/EnCore specifics from the MES domain model.
- Reconciliation workflows to compare CoroMES state against ERP and legacy-system data during migration.

## Legacy Module Mapping

| Legacy Capability | CoroMES Replacement Direction |
| --- | --- |
| Corrugator scheduling and trim optimization | Scheduling module with optimization rules and schedule versioning |
| Converting schedules and work center balancing | Scheduling module with capacity/load model |
| Operator job tracking | Production/floor execution module with tablet UI |
| Scrap and downtime entry | Quality/production event capture with reason codes |
| Roll stock receiving and movement | Inventory module with roll lifecycle tracking |
| CorrTrac clamp truck interface | Forklift PWA/mobile workflow with scan validation |
| Wet-end roll usage and butt rolls | Inventory + equipment telemetry + traceability |
| IntCorr and bridge databases | ERP integration service and event bus |
| Workstation-specific permissions | Central identity, roles, plant/machine permissions |
| Modicon/custom driver configuration | OPC-UA, MQTT, Ethernet/IP, and managed edge config |

## Near-Term Implementation Focus

The immediate project focus is to replace MES and SCADA functions first:

- Treat the legacy system as a possible source of truth during migration.
- Build adapters that can read from existing CTI/EPS, SQL, bridge, or file sources where necessary.
- Build a separate CTI-to-CoroMES connector program inside the solution for ingestion, mapping, reconciliation, and sync monitoring.
- Start with high-value operational workflows: work orders, machine status, production events, roll inventory, and floor displays.
- Keep ERP integration bounded behind an integration service.
- Avoid coupling the new CoroMES domain model directly to legacy table structures.
- Design the MCP surface early, starting read-only with work orders, equipment, materials, quality records, production events, reports, and i3X object/value/history lookup.
- Keep standard machine/MES communication translators modular, with each translator mapping its source into CoroMES/i3X object, relationship, value, and history semantics.

## Key Design Principle

Do not perform a screen-for-screen rewrite of CTI/EPS. Preserve the business-critical workflows and rules, but rebuild them around modern services, event-driven integration, auditable data, browser-based interfaces, and resilient plant-edge connectivity.
