# Architectural Decisions

## ADR-001: Solution Name
- **Date:** 2026-04-16
- **Decision:** CoroMES (homebrew MES for manufacturing)
- **Reason:** User preference - homebrew Manufacturing Execution System
- **Status:** Accepted

## ADR-002: Framework Choice
- **Date:** 2026-04-16
- **Decision:** .NET 10 Minimal API
- **Reason:** User requested C#/.NET, Minimal API for lightweight REST endpoints
- **Alternatives considered:** Python/Flask, Node.js, PHP
- **Status:** Accepted

## ADR-003: Database Choice
- **Date:** 2026-04-16
- **Decision:** PostgreSQL
- **Reason:** User preference for Postgre ("Postgre")
- **Alternatives considered:** SQL Server, SQLite (dev only)
- **Status:** Accepted

## ADR-004: API Strategy
- **Date:** 2026-04-16
- **Decision:** Separate Core API and Reporting API
- **Reason:** Different consumers (operators/APPs vs BI tools like Power BI/Tableau)
- **Core API:** /api/v1/* for CRUD operations
- **Reporting API:** /reporting/v1/* for analytics, aggregations, CSV export
- **Status:** Accepted

## ADR-005: Industrial Protocols (Out of Box)
- **Date:** 2026-04-16
- **Decision:** Support MQTT, OPC-UA, Ethernet/IP natively
- **Reason:** User requirements for shop floor connectivity
- **MQTT:** Sensors, IoT devices (MQTTnet)
- **OPC-UA:** SCADA, Siemens, HMI (OPC Foundation SDK)
- **Ethernet/IP:** Allen-Bradley PLCs (EthernetIP library)
- **Status:** Accepted

## ADR-006: External Integrations
- **Date:** 2026-04-16
- **Decision:** TrueCommerce (EDI), Upkeep.com (CMMS)
- **Reason:** User has existing TrueCommerce setup for EDI; Upkeep for maintenance
- **Status:** Accepted

## ADR-007: Project Structure (Clean Architecture)
- **Date:** 2026-04-16
- **Decision:** Clean Architecture with Modular domains
- **Core:** Entities, interfaces, enums
- **Application:** Use cases, DTOs, services
- **Infrastructure:** EF Core, external clients
- **Modules:** Production, Quality, Inventory, Equipment, Workforce
- **Reason:** Industry best practice for MES systems (MESA model)
- **Status:** Accepted

## ADR-008: Versioning Strategy
- **Date:** 2026-04-16
- **Decision:** Semantic Versioning (SemVer) with API versioning
- **Format:** Major.Minor.Patch (e.g., 1.0.0)
- **API Versioning:** URL-based (/api/v1/, /reporting/v1/)
- **Status:** Accepted

## ADR-009: i3X Standards Compliance
- **Date:** 2026-05-28
- **Updated:** 2026-06-10
- **Decision:** CoroMES i3X integration must follow the official CESMII i3X 1.0 specification rather than endpoint behavior from any single non-compliant or beta-era server.
- **Reason:** i3X is intended as a vendor-agnostic REST API for contextualized manufacturing data. Some test endpoints may advertise capabilities before implementing the standard routes correctly.
- **Canonical references:** https://github.com/cesmii/i3X/tree/1.0, https://github.com/cesmii/i3X/blob/1.0/spec/IMPLEMENTATION_GUIDE.md, https://api.i3x.dev/v1/openapi.json, and `docs/I3X_STANDARDS_TRACKING.md`
- **Latest upstream check:** branch `1.0` at `bbaded54e057718916e2ed5d67cd065f691799d3`; tag `1.0.0` at `34b766442f6ef614d47fe905459a2ea8b91c6f8b`.
- **Required read/explore routes:** `GET /namespaces`, `GET /objecttypes`, `POST /objecttypes/query`, `GET /relationshiptypes`, `POST /relationshiptypes/query`, `GET /objects`, `POST /objects/list`, `POST /objects/related`
- **Required value/history routes:** `POST /objects/value`, `POST /objects/history`
- **Required update routes:** `PUT /objects/value`, `PUT /objects/history`
- **Required subscription routes:** `POST /subscriptions`, `POST /subscriptions/list`, `POST /subscriptions/delete`, `POST /subscriptions/register`, `POST /subscriptions/unregister`, `POST /subscriptions/sync`; `POST /subscriptions/stream` is optional.
- **Implementation note:** Subscription requests require `clientId`; timestamps must be UTC with `Z`; error payloads use `responseDetail`; streaming is optional, so collectors must support polling/sync first.
- **Status:** Accepted

## ADR-010: MCP Exposure for AI Agents
- **Date:** 2026-05-28
- **Decision:** CoroMES should expose MES data and selected operations through a Model Context Protocol (MCP) server so AI agents can browse, query, and act on manufacturing context safely.
- **Reason:** AI agents should be able to interact with work orders, equipment, materials, quality, production events, reporting data, and i3X-contextualized shop-floor data through a stable tool/resource interface rather than scraping application screens or calling internal APIs ad hoc.
- **Architecture:** Add a dedicated MCP host or module that sits beside the REST/reporting APIs and reuses application services/repositories. It should not bypass domain rules, authorization, audit logging, or validation.
- **Initial MCP surface:** read-only resources/tools for work orders, equipment, materials, machine status, production events, roll inventory, quality records, reports, and i3X object/value/history lookup.
- **Write-capable tools:** allowed later, but must be explicit, narrowly scoped, permission-aware, auditable, and designed for human approval where needed.
- **Security:** require authentication/authorization, plant/role scoping, rate limits, structured logging, and guardrails for destructive or externally posted actions.
- **Status:** Accepted

## ADR-011: CTI Connector and Modular i3X Translators
- **Date:** 2026-05-28
- **Decision:** Build the CTI-to-CoroMES integration as a separate program within the CoroMES solution, using CTI as a migration/operational data source while CoroMES is phased in.
- **Reason:** The current CTI system contains valuable live and historical MES/SCADA context. Keeping the connector separate reduces coupling, makes migration safer, and allows CTI access logic to evolve independently from the CoroMES domain services.
- **Architecture:** Add a dedicated connector project/module for CTI ingestion, mapping, reconciliation, and sync status. It should publish normalized data/events into CoroMES APIs/services rather than letting core modules depend directly on CTI tables, files, screens, or proprietary interfaces.
- **Research baseline:** Initial Welch CTI/EPS facts and public product research are captured in `docs/CTI_RESEARCH_BRIEF.md`. Public research links CTI to ePS Corrugated Suite / CorrSuite terminology and module names including CorrPlan, CorrTrim, CorrTrac, CorrLink, CorrChain, pkgWARE, CBS, Escada, Auto-Count, and PC-Topp. Future CTI discovery and code should search for both Welch-specific names and these historical/current product names.
- **Agent support:** A reusable Codex skill, `$welch-cti-connector`, was created at `C:\Users\soperbp\.codex\skills\welch-cti-connector` and validated with `quick_validate.py`. Use it for future CTI/EPS discovery, design, parser, mapping, reconciliation, and runbook work.
- **Integration guardrail:** Start with file-based, read-only extraction that mirrors the legacy CTI/Amtech boundary, then add read-only database extraction only for domains where files are incomplete, delayed, or lossy. Preserve raw payloads, correlation IDs, source hashes, and reconciliation evidence.
- **Framework start:** Initial connector framework lives in `integration/CoroMES.Integration.Cti` as a .NET 10 class library with file discovery, immutable raw capture, conservative parsing, validation, quarantine, DI registration, and unit tests.
- **Translator design:** Standard machine/MES communication translators must be modular. Each translator should convert a specific source/protocol/model into the CoroMES/i3X context model through a shared abstraction.
- **Initial translator targets:** CTI, MQTT, OPC-UA, Ethernet/IP, and other machine/MES sources as needed.
- **i3X alignment:** Translator output should preserve i3X object type, object instance, relationship, value, and history semantics so CoroMES can expose consistent i3X and MCP views.
- **Status:** Accepted
