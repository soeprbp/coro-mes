# Quick Context

## What is CoroMES?
Custom Manufacturing Execution System (MES) for Welch Packaging.

## Tech Stack
- **Framework:** .NET 10 Minimal API
- **Database:** PostgreSQL
- **Architecture:** Clean Architecture + Modular

## Key Integrations
- **TrueCommerce:** EDI (X12 850/810) - existing setup
- **CTI Connector:** Planned separate CoroMES program for using current CTI/EPS as a data source during migration. Use `$welch-cti-connector` in future Codex sessions; see ADR-011 and `docs/CTI_RESEARCH_BRIEF.md` for Welch-specific facts, public CTI/ePS product lineage, module names, discovery search terms, and connector guardrails.
- **Upkeep.com:** CMMS API
- **i3X:** Follow CESMII public beta/OpenAPI standard for contextualized manufacturing data. Use `memory/DECISIONS.md` ADR-009 as the endpoint checklist before changing i3X client/server code.
- **MCP:** Expose MES data and selected operations through an MCP server for AI agents. Use `memory/DECISIONS.md` ADR-010 before designing agent-facing tools/resources.
- **Industrial:** MQTT, OPC-UA, Ethernet/IP

## Project Structure
```
src/          - Core + API layer
modules/      - MES domains (Production, Quality, Inventory, Equipment, Workforce)
integration/  - External APIs (EDI, CMMS, IIoT)
industrial/   - Protocol handlers (MQTT, OPC-UA, EIP)
```

## Quick Links
- **Docs:** ./docs/
- **Config:** ./config/settings.yaml
- **Memory:** ./memory/
- **Changelog:** ./CHANGELOG.md

## Development Commands
```powershell
# Build
dotnet build

# Run
dotnet run --project src/CoroMES.Api

# Test
dotnet test
```

## Current Status
- Solution created with 19 projects
- No implementation yet (just structure)
- Next: Add references and packages

## Long-Term Direction
- Replace legacy CTI/EPS MES and SCADA functions with CoroMES first.
- The current MES/SCADA system may be used as a temporary migration data source.
- See `memory/LONG_TERM_GOALS.md` for the modernization plan and legacy-module mapping.
