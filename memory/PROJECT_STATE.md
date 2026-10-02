# CoroMES deployment state

Updated October 2, 2026. This describes the master-based prototype deployment, not the newer `codex/blazor-forward` branch.

The deployed application is the .NET 10 Minimal API with three static web pages. It runs in Docker on a Debian 13 Hyper-V VM, alongside Portainer and a separate historical CTI dashboard. SQLite uses a persistent volume. Browser requests use the current origin instead of a fixed development API address.

Container build and HTTP checks passed. Two VM reboot checks verified service startup and tunnel recovery. API/Infrastructure dependency checks reported no known advisories from current NuGet sources. A verified database backup is retained privately; automatic off-host backups remain to be configured.

The database is empty. The admin login is not server-side authentication; display endpoints are placeholders, the OEE endpoint is missing, and the viewer falls back to demo content. Builder layouts remain in browser localStorage. No live CTI collector, industrial connection, or partner integration runs here.

Remote branch `codex/blazor-forward` has substantial newer work, including a Blazor host and persisted displays. It was discovered during the Git refresh after deployment and has not been merged or deployed. Review it before extending the older UI or calling the whole CoroMES application migrated.

Use `deploy/portable/compose.yaml`. See [portable operations](../docs/PORTABLE_DEPLOYMENT.md) and untracked `docs/DEPLOYMENT.local.md` for the private handoff. Earlier architecture decisions describe goals and do not establish implemented behavior.
