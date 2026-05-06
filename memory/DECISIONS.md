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