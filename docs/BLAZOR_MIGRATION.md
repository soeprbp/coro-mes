# Blazor Migration

**Last Updated:** 2026-06-10

## Purpose

This Blazor fork makes `src/CoroMES.Web` the forward user-facing host for CoroMES while preserving the existing Minimal API backend contract. The goal is to replace the static admin and display prototypes with a maintainable Blazor application without losing route parity, migration history, or access to the pre-Blazor source state.

## Preservation Points

- Preservation branch/tag: `pre-blazor-2026-06-10`
- Source backup zip: `C:\Users\soperbp\OneDrive - Welch Packaging Group\Scripts\workdev\CoroMES-source-backup-2026-06-10.zip`
- Blazor fork workspace: `C:\Users\soperbp\OneDrive - Welch Packaging Group\Scripts\workdev\CoroMES-blazor`

Use the preservation branch/tag and backup zip as the reference point for the pre-Blazor Minimal API plus static web implementation.

## Forward Host

- `src/CoroMES.Web` is the forward Blazor host for admin and shop-floor display UX.
- `src/CoroMES.Api` remains the backend API host and source of truth for implemented HTTP route behavior.
- Shared domain, persistence, CTI, i3X, and integration boundaries remain in the existing projects unless a future migration deliberately moves them.

## API Route Parity

The Blazor app should preserve the current API route contract while it replaces static pages. Existing route families remain the compatibility target:

```text
GET    /health
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
GET    /api/v1/displays
GET    /api/v1/displays/{id}
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
- `src/CoroMES.Web` starts locally as the forward UI host.
- `src/CoroMES.Api` still exposes the documented API route set.
- Old static URLs redirect or remain compatible.
- Admin and display workflows remain reachable through Blazor routes.
