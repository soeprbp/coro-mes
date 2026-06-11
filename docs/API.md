# API Overview

This document reflects the API surface that is actually implemented in the Blazor-forward `src/CoroMES.Web` host and the compatibility `src/CoroMES.Api` host. The Blazor fork should preserve this backend route contract as UI screens move.

## Current Runtime Surface

The repository currently exposes a single Minimal API host.

- Base host: `CoroMES.Api`
- Forward UI host: `CoroMES.Web`
- Main route family: `/api/v1`
- Health check: `/health`
- Legacy static admin URL: `/admin`
- Legacy static display URLs: `/displays/viewer.html`, `/displays/builder.html`

In `CoroMES.Web`, `/api/v1/*` is protected by the first admin auth gate. `/health` remains anonymous. The standalone `CoroMES.Api` host remains a compatibility/reference host and still needs equivalent auth before any non-local exposure.

## Implemented Endpoints

### Health

```text
GET /health
```

Returns a simple health payload with status and timestamp.

### Work Orders

```text
GET  /api/v1/workorders
GET  /api/v1/workorders/{id}
POST /api/v1/workorders
```

Notes:

- list responses are capped to the first 100 records
- create assigns a generated work order number and `CreatedAt`
- update, delete, start, complete, and scrap endpoints are not implemented yet

### Equipment

```text
GET    /api/v1/equipment
GET    /api/v1/equipment/{id}
POST   /api/v1/equipment
PUT    /api/v1/equipment/{id}
DELETE /api/v1/equipment/{id}
```

Notes:

- this is one of the more complete resource areas in the current API
- the admin HTML prototype primarily exercises this surface
- equipment create/update/delete actions in `CoroMES.Web` write audit log entries

### Audit

```text
GET /api/v1/audit
GET /api/v1/audit?entityName=Equipment&take=100
```

Notes:

- this endpoint is admin-protected in `CoroMES.Web`
- returns recent audit records newest first
- first-pass audit coverage records equipment create/update/delete, display definition create/update, UpKeep sync/downtime actions, alarm lifecycle actions, and settings update/rejection actions

### Alarms

```text
GET  /api/v1/alarms
GET  /api/v1/alarms?status=Active&take=100
GET  /api/v1/alarms/{id}
POST /api/v1/alarms
POST /api/v1/alarms/{id}/acknowledge
POST /api/v1/alarms/{id}/resolve
```

Notes:

- these endpoints are admin-protected in `CoroMES.Web`
- alarm events persist in `ApplicationDbContext.AlarmEvents`
- create accepts title, message, severity, source, optional equipment id, and optional alert channel list
- alert dispatch goes through `CoroMES.Integration.Alerts`
- local default mode is mock; critical alarms default to email, SMS, Pushover, and UpKeep channels
- live alert dispatch is guarded and returns blocked results until provider credentials, send contracts, throttling, and operator escalation rules are configured
- create, acknowledge, and resolve actions write audit records

### Settings

```text
GET /api/v1/settings
PUT /api/v1/settings
```

Notes:

- these endpoints are admin-protected in `CoroMES.Web`
- settings persist in `ApplicationDbContext.SystemSettings`
- only allowlisted non-secret integration endpoints, protocol settings, and feature flags can be saved
- secret-like values such as keys, tokens, passwords, connection strings, bearer strings, and RTSP URLs are rejected
- secret status is reported as configured/missing without returning the secret value
- settings update and rejected update attempts write audit records

### Materials

```text
GET  /api/v1/materials
GET  /api/v1/materials/{id}
POST /api/v1/materials
```

Notes:

- despite older docs, the implemented route prefix is `/materials`, not `/inventory`
- inventory movement and BOM endpoints are not implemented yet

### Operators

```text
GET  /api/v1/operators
GET  /api/v1/operators/{id}
POST /api/v1/operators
```

Notes:

- despite older docs, the implemented route prefix is `/operators`, not `/workforce/operators`

### Quality

```text
GET /api/v1/quality/inspections
GET /api/v1/quality/ncr
```

Notes:

- these are read-only list endpoints today
- create/update inspection and NCR workflows are not implemented yet

### Upkeep Integration

```text
GET  /api/v1/integration/upkeep/assets
POST /api/v1/integration/upkeep/sync
POST /api/v1/integration/upkeep/downtime
```

Notes:

- these endpoints call the `CoroMES.Integration.Upkeep` boundary
- default local mode is mock and returns the seeded UpKeep asset catalog used by the Blazor matching UI
- disabled mode returns no assets and rejects sync/downtime requests
- live mode is guarded behind `Upkeep:Mode=live`, `Upkeep:BaseUrl`, and `Upkeep:ApiKey`
- live asset reads have an adapter slot, but live sync and downtime writes remain blocked until the final UpKeep write API contract is confirmed
- sync and downtime calls write audit records with success/failure status

### Displays

```text
GET  /api/v1/displays
GET  /api/v1/displays/{id}
POST /api/v1/displays
PUT  /api/v1/displays/{id}
```

Notes:

- display definitions are persisted in `ApplicationDbContext.DisplayDefinitions`
- `id` is the display slug
- default display definitions are seeded at startup when no display definitions exist
- the Blazor display builder writes saved definitions and the viewer loads them by slug

## Blazor Route Parity and Redirects

`CoroMES.Web` should call or preserve the implemented API route families above. UI work in the Blazor fork should not rename backend routes from `/materials` to `/inventory`, from `/operators` to `/workforce/operators`, or otherwise drift from the live Minimal API contract without an explicit API migration.

Old static URLs should remain compatible during migration:

| Legacy URL | Forward Blazor Route |
|------------|----------------------|
| `/admin` | `/admin` |
| `/admin/` | `/admin` |
| `/displays/viewer.html` | `/displays/viewer` |
| `/displays/builder.html` | `/displays/builder` |

The pre-Blazor API/static web state is preserved at branch/tag `pre-blazor-2026-06-10` and in `C:\Users\soperbp\OneDrive - Welch Packaging Group\Scripts\workdev\CoroMES-source-backup-2026-06-10.zip`.

## Configuration-Driven Behavior

### Database provider

The API chooses persistence mode at startup:

- default local mode: SQLite
- alternative mode: PostgreSQL

### Optional i3X repository mode

When `i3x.enabled` is true, repository registrations switch from EF Core-backed repositories to i3X-backed repositories.

This changes where data is read and written without changing the route surface.

## Not Implemented Yet

The following surfaces were described in earlier docs but are not implemented in the current API host:

- reporting routes under `/reporting/v1`
- industrial routes under `/industrial/v1`
- EDI processing endpoints
- `/api/v1/inventory/*`
- `/api/v1/workforce/*`
- work order lifecycle actions such as start and complete
- quality write endpoints
- enterprise identity integration and rate limiting
- audit coverage beyond current equipment, display, UpKeep, alarm, and settings mutations

## Response Style

The current API is pragmatic and prototype-oriented:

- plain JSON payloads
- no standard envelope across all endpoints
- no paging contract beyond local `Take(100)` usage on some lists
- first admin auth gate in `CoroMES.Web`; standalone `CoroMES.Api` still needs parity before exposure

## Testing Status

Current automated coverage is strongest around:

- CTI connector components
- i3X client behaviors
- Blazor auth, audit, display, UpKeep, alarm, and settings smoke coverage

There is not yet broad end-to-end API integration coverage for the full route set.

## Recommended Next API Documentation Step

If the route surface expands further, the next useful improvement would be to split this file into:

1. `API_CURRENT.md` for the implemented surface
2. `API_PLANNED.md` for future reporting, industrial, and workflow endpoints

For now, this file serves as the source of truth for the live API.
