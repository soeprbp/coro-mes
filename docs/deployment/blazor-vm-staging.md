# CoroMES Blazor VM staging

This profile builds `src/CoroMES.Web` (.NET 10 Blazor) as a separate service on
the CoroMES VM. It uses a separate SQLite volume, so the existing prototype can
stay available during verification. The deployment may seed this volume with a
consistent prototype backup before first startup; it never shares the original
database file. Validate the API reads before promoting an existing database.

## Start

Keep the admin access code in a private file **outside** the repository. Set
`COROMES_ADMIN_CODE_FILE` to that absolute path in the shell running Compose.
The file is mounted as `/run/secrets/Auth__AdminAccessCode`; `Program.cs` loads
that value through ASP.NET configuration. Never add the code to Compose, Git,
or command output. The container user must be able to read the mounted file:
check its numeric UID with `docker run --rm --entrypoint id coromes-blazor:staging -u`
after building, then give that UID read access to the secret. Keep the containing
host directory accessible only to root and do not make the secret world-readable.

```sh
export COROMES_ADMIN_CODE_FILE=/path/to/private/admin-code
docker compose -p coromes-blazor -f infra/docker/blazor-staging.compose.yml up -d --build
docker compose -p coromes-blazor -f infra/docker/blazor-staging.compose.yml ps
curl -f http://127.0.0.1:5101/health
```

The staging URL is `http://127.0.0.1:5101/` on the VM. Use an SSH tunnel to
view it from another computer. The Compose port can be changed with
`COROMES_STAGING_PORT`. Keep it bound to loopback while the single shared
access-code login is in use.

## Storage and isolation

### Optional direct internal-network address

For an explicitly approved LAN deployment, set `COROMES_LAN_ADDRESS` to the
guest's LAN IP in the private deployment environment file, then include
`-f infra/docker/blazor-lan.compose.yml` after the base Compose file on every
`up` command. This adds port 80 on that interface and preserves loopback 5101.
Users can open `http://<guest-hostname>/admin` without an SSH tunnel. The existing
admin login still applies. HTTP does not encrypt access codes or session cookies;
use trusted HTTPS before access outside the trusted internal network. This
override does not expose Portainer or enable external application integrations.
To return to loopback-only access, recreate the gateway using only the base file.

The `coromes-blazor_coromes_blazor_data` Docker volume holds both
`/app/data/coromes.db` and `/app/data/keys` (the ASP.NET Data Protection key
ring). Back up both together. The app runs as the .NET image's unprivileged
`app` user, without Linux capabilities or privilege escalation. Compose uses
an internal Docker network to block app-initiated external connections. A fixed
nginx gateway joins that network and a separate ingress bridge, publishing only
the VM's loopback port. WebSocket forwarding supports Blazor's interactive
connection. Compose restarts the gateway when it updates the application, so
nginx does not retain an obsolete container address.

Runtime configuration sets `DatabaseProvider=sqlite`, `i3x:enabled=false`,
`MesVisionCollector:Enabled=false`, `Upkeep:Mode=disabled`, and
`Alerting:Mode=disabled`. The UI creates three example display definitions
in a fresh database; they are demo layouts and do not imply a live CTI feed.
The manual MES Vision collection endpoint still exists; do not invoke it
during isolated staging.

## Checks and recovery

Check `/health` for HTTP 200, `/admin` for a login redirect, and a protected
`/api/v1/equipment` request for HTTP 401 before signing in. Then verify an
admin read in the browser with the configured access code. Container health
and logs are available through `docker compose ... ps` and `docker compose
... logs --tail=100 coromes-blazor`. Avoid logging the access code.

To stop only this service, run:

```sh
docker compose -p coromes-blazor -f infra/docker/blazor-staging.compose.yml down
```

`down` retains the named volume. For a consistent backup, stop the service
first and copy the database and key ring from the volume; then restart it.
Do not run `down -v` unless the staged data has been deliberately retired.
The previous prototype remains the rollback service until the staged Blazor
app is verified and the display links are deliberately switched.
