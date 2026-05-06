# Quick Context

## What is CoroMES?
Custom Manufacturing Execution System (MES) for Welch Packaging.

## Tech Stack
- **Framework:** .NET 10 Minimal API
- **Database:** PostgreSQL
- **Architecture:** Clean Architecture + Modular

## Key Integrations
- **TrueCommerce:** EDI (X12 850/810) - existing setup
- **Upkeep.com:** CMMS API
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