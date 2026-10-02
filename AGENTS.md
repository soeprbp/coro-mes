# CoroMES working context

- Read `memory/PROJECT_STATE.md` and `docs/PORTABLE_DEPLOYMENT.md` before deployment work. This package is the master-based prototype; `codex/blazor-forward` has newer work requiring compatibility and data migration review.
- Deploy this version with `deploy/portable/compose.yaml`. Preserve its SQLite volume; never use `down --volumes` as an update command.
- Keep the API loopback-bound: the static login prompt is not authentication and write routes are unprotected. Demo displays and HTTP health are not live MES validation.
- Never connect to or probe production databases, OT endpoints, or external integrations without explicit current-conversation approval of endpoint and purpose. Past bounded pilot approval does not authorize collectors or additional queries.
- Use native Codex agents for delegated work. External/local AI workers require explicit user authorization.
- This repository is public. Never commit credentials, SSH keys, internal database data, source snapshots, CAD files, or site-specific host inventories. Local operations details belong in ignored `docs/DEPLOYMENT.local.md`.
- Preserve unrelated local files. Verify source/Compose changes appropriately, maintain the Markdown guide and accurate comments, and consult the newer branch before adding features to the old UI.
