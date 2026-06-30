# Blazor Migration

**Last Updated:** 2026-06-10

## Purpose

This Blazor fork makes `src/CoroMES.Web` the forward user-facing host for CoroMES while preserving the existing Minimal API backend contract. The goal is to replace the static admin and display prototypes with a maintainable Blazor application without losing route parity, migration history, or access to the pre-Blazor source state.

## Preservation Points

- Preservation branch/tag: `pre-blazor-2026-06-10`
- Source backup zip: `C:\scripts\coroMES\CoroMES-source-backup-2026-06-10.zip`
- Blazor fork workspace: `C:\scripts\coroMES\CoroMES-blazor`
- Preserved pre-Blazor workspace: `C:\scripts\coroMES\CoroMES`

Use the preservation branch/tag and backup zip as the reference point for the pre-Blazor Minimal API plus static web implementation.
The CoroMES workspace was moved out of OneDrive on 2026-06-18. Keep new CoroMES work under `C:\scripts\coroMES`.

## Forward Host

- `src/CoroMES.Web` is the forward Blazor host for admin and shop-floor display UX.
- `src/CoroMES.Api` remains the backend API host and source of truth for implemented HTTP route behavior.
- Shared domain, persistence, CTI, i3X, and integration boundaries remain in the existing projects unless a future migration deliberately moves them.

## First Auth Gate

The Blazor forward host now uses ASP.NET Core cookie authentication as the first admin safety gate.

- Anonymous: `/health`, `/login`, `/access-denied`, `/displays/viewer`, `/displays/viewer.html`, static assets, not-found/error handling.
- Admin-required: `/admin`, `/admin/*`, `/displays`, `/displays/builder`, `/api/v1/*`.
- Development fallback access code: `dev-admin` when `Auth:AdminAccessCode` is not configured.
- Production/admin override: set `Auth__AdminAccessCode` through environment variables, user secrets, or deployment configuration. Do not commit real access codes.

This is a migration gate, not the final identity model. Before production use, replace or extend it with the chosen plant/user identity provider, stronger role mapping, full audit coverage for mutations, and a CSRF strategy for any browser-called JSON mutation endpoints.

## Audit Logging

The first audit foundation records equipment create/update/delete actions from both the Blazor admin page and the JSON API. It now also covers display definition create/update actions, UpKeep sync/downtime placeholder actions, alarm create/acknowledge/resolve actions, settings update/rejection actions, and MES-Vision camera/zone equipment mapping actions.

- Entity: `AuditLog`
- Storage: `ApplicationDbContext.AuditLogs`
- Read endpoint: `GET /api/v1/audit`
- Covered entities today: `Equipment`, `DisplayDefinition`, `AlarmEvent`, `SystemSetting`, `VisionCamera`, `VisionZone`, and first-pass `Upkeep` integration actions

The next audit expansion should cover work orders, materials, operators, persisted settings changes, and any future i3X or industrial write paths.

## Modular Host Shape

`src/CoroMES.Web` now keeps startup small and routes behavior through focused modules:

- `Endpoints/*Endpoints.cs` maps auth, compatibility redirects, audit, alarms, settings, work orders, equipment, materials, operators, quality, UpKeep, and display endpoints.
- `Startup/DatabaseStartupExtensions.cs` owns startup-time database safety checks and seed behavior for Blazor-host tables.
- `Program.cs` stays responsible for service registration, middleware order, and calling the endpoint composition methods.

This is the first modularization step. The next extraction should move heavier business workflows from endpoint lambdas into application services once the route surface settles.

## Next Build Slice

The team should move the Blazor fork forward in this order:

1. Keep the auth and audit integration smoke suite green as a migration guardrail.
2. Replace guarded alerting send stubs with provider-specific adapters only after credentials, throttling, and escalation rules are reviewed.
3. Add dashboard/reporting widgets from mapped MES-Vision camera/zone telemetry.
4. Add UpKeep live asset-read compatibility once credentials and API details are available.
5. Start moving endpoint behavior into application services where workflows are no longer simple CRUD.

This order keeps Jane's security surface reviewable while Wash and River expand the host without burying business behavior inside the startup file.

## API Route Parity

The Blazor app should preserve the current API route contract while it replaces static pages. Existing route families remain the compatibility target:

```text
GET    /health
GET    /api/v1/audit
GET    /api/v1/alarms
GET    /api/v1/alarms/{id}
POST   /api/v1/alarms
POST   /api/v1/alarms/{id}/acknowledge
POST   /api/v1/alarms/{id}/resolve
GET    /api/v1/settings
PUT    /api/v1/settings
GET    /api/v1/workorders
GET    /api/v1/workorders/{id}
POST   /api/v1/workorders
GET    /api/v1/equipment
GET    /api/v1/equipment/{id}
POST   /api/v1/equipment
PUT    /api/v1/equipment/{id}
DELETE /api/v1/equipment/{id}
GET    /api/v1/materials
GET    /api/v1/materials/{id}
POST   /api/v1/materials
GET    /api/v1/operators
GET    /api/v1/operators/{id}
POST   /api/v1/operators
GET    /api/v1/quality/inspections
GET    /api/v1/quality/ncr
GET    /api/v1/integration/upkeep/assets
POST   /api/v1/integration/upkeep/sync
POST   /api/v1/integration/upkeep/downtime
POST   /api/v1/integration/mes-vision/collect
GET    /api/v1/integration/mes-vision/sources
GET    /api/v1/integration/mes-vision/mappings
PUT    /api/v1/integration/mes-vision/cameras/{id}/equipment
PUT    /api/v1/integration/mes-vision/zones/{id}/equipment
GET    /api/v1/integration/mes-vision/readings
GET    /api/v1/integration/mes-vision/events
GET    /api/v1/displays
GET    /api/v1/displays/{id}
POST   /api/v1/displays
PUT    /api/v1/displays/{id}
```

When Blazor screens are added or rewritten, they should call or preserve these backend routes instead of silently changing resource names, casing, or route prefixes.

## Static URL Redirects

The old static URLs should remain friendly entry points during migration:

```text
/admin
/admin/
/displays/viewer.html
/displays/builder.html
```

Expected behavior is to redirect those URLs to the matching Blazor routes, or to keep serving a compatibility shim until the Blazor route is complete. Do not break bookmarked shop-floor display links without a redirect or documented cutover.

Suggested forward mappings:

| Old URL | Forward Blazor Route |
|---------|----------------------|
| `/admin` | `/admin` |
| `/admin/` | `/admin` |
| `/admin/alarms` | `/admin/alarms` |
| `/displays/viewer.html` | `/displays/viewer` |
| `/displays/builder.html` | `/displays/builder` |

## Migration Guardrails

- Keep API route parity unless a breaking change is explicitly planned and documented.
- Replace static pages incrementally with Blazor components.
- Leave CTI/EPS migration logic isolated from UI concerns.
- Treat i3X repository mode as a backend data-source decision, not a UI-specific concern.
- Keep redirect behavior visible in docs when old static paths move.

## Verification Checklist

- `dotnet build CoroMES.sln` succeeds.
- `dotnet test CoroMES.sln` succeeds, including Blazor auth and audit integration tests.
- `src/CoroMES.Web` starts locally as the forward UI host.
- `src/CoroMES.Api` still exposes the documented API route set.
- Old static URLs redirect or remain compatible.
- Anonymous requests to `/admin/equipment` redirect to `/login`.
- Anonymous requests to `/api/v1/equipment` return `401`.
- Signed-in admin requests can reach `/admin/equipment` and `/api/v1/equipment`.
- Equipment create/update/delete writes audit records.
- Display definitions persist through `/api/v1/displays` and load in `/displays/viewer?id={slug}`.
- UpKeep asset lookup, sync, and downtime calls go through `CoroMES.Integration.Upkeep` and write audit records.
- MES-Vision manual collection polls the configured read-only i3X endpoint and persists source, camera, zone, reading, and event records.
- MES-Vision mapping endpoints and `/admin/vision` link collected cameras/zones to CoroMES equipment and write audit records.
- Alarm create, acknowledge, and resolve calls persist `AlarmEvent` records, call `CoroMES.Integration.Alerts`, and write audit records.
- Settings updates persist allowlisted non-secret `SystemSetting` records by user, reject secret-looking values, report secret status without exposing values, and write audit records.
- `/displays/viewer?id=preview&type=oee` remains reachable without admin sign-in.
