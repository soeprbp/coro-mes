# API Overview

This document reflects the API surface that is actually implemented in `src/CoroMES.Api` today.

## Current Runtime Surface

The repository currently exposes a single Minimal API host.

- Base host: `CoroMES.Api`
- Main route family: `/api/v1`
- Health check: `/health`
- Static admin UI: `/admin`
- Static display assets: `/displays/*`

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

### Upkeep Integration Placeholders

```text
GET  /api/v1/integration/upkeep/assets
POST /api/v1/integration/upkeep/sync
POST /api/v1/integration/upkeep/downtime
```

Notes:

- these endpoints are placeholders and prototype behavior
- they do not yet call a live Upkeep API

### Displays

```text
GET /api/v1/displays
GET /api/v1/displays/{id}
```

Notes:

- these currently return hard-coded display configuration payloads

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
- authentication and rate limiting

## Response Style

The current API is pragmatic and prototype-oriented:

- plain JSON payloads
- no standard envelope across all endpoints
- no paging contract beyond local `Take(100)` usage on some lists
- no auth layer in development mode

## Testing Status

Current automated coverage is strongest around:

- CTI connector components
- i3X client behaviors

There is not yet broad end-to-end API integration coverage for the full route set.

## Recommended Next API Documentation Step

If the route surface expands further, the next useful improvement would be to split this file into:

1. `API_CURRENT.md` for the implemented surface
2. `API_PLANNED.md` for future reporting, industrial, and workflow endpoints

For now, this file serves as the source of truth for the live API.
