# Portable deployment of the master prototype

This package deploys the API and static pages from `master`, based on commit `18987ad`. The separate `codex/blazor-forward` branch contains the newer Blazor application and has not been deployed with this package. This deployment does not migrate that newer branch.

The host is a Debian 13 Hyper-V VM with Docker Engine and Portainer. Initial resources are 4 virtual CPUs, 8 GB RAM, and an 80 GB expanding disk. Configure the VM to start at host boot. Keep addresses, credentials, SSH keys, backups, and site-specific instructions in the untracked `docs/DEPLOYMENT.local.md` and private operations storage.

## Services and persistence

`deploy/portable/compose.yaml` runs the .NET API and static pages in one container. SQLite lives in named volume `coromes_coromes_data`; `CoroMES__SqlitePath` selects its path. The container runs as the image's unprivileged user, with dropped capabilities and a loopback-only host port.

Portainer and the historical CTI board are separate services. This repository does not publish the board's internal data. PostgreSQL, MQTT, and industrial/partner projects in this branch are scaffolding, not active connectors. Starting this Compose package does not contact production databases or OT devices.

## Build and operate

Install Docker Engine and its Compose plugin on the Linux host. From this checkout:

```bash
sudo docker compose -f deploy/portable/compose.yaml config --quiet
sudo docker compose -f deploy/portable/compose.yaml up -d --build
sudo docker compose -f deploy/portable/compose.yaml ps
curl --fail http://127.0.0.1:5100/health
curl --fail http://127.0.0.1:5100/api/v1/equipment
```

Open `/admin/index.html`, `/displays/builder.html`, or `/displays/viewer.html` through an SSH tunnel. For example, on an administrator workstation:

```bash
ssh -N -L 5270:127.0.0.1:5100 admin@your-container-host
```

Then open `http://127.0.0.1:5270/admin/index.html`. `COROMES_PORT` overrides the VM's default 5100 port. Keep the listener on loopback until server-side authentication protects all write routes. Portainer can inspect the CLI-created stack; Compose remains the configuration source of truth.

For updates, preserve the volume and run `up -d --build` again. Do not use `down --volumes`. Inspect failures with:

```bash
sudo docker compose -f deploy/portable/compose.yaml logs --tail=100 api
```

The root and `infra/docker` compose files are older development sketches, not the deployed configuration.

## Backup and server move

`scripts/backup_sqlite.py SOURCE DESTINATION` opens the source read-only and produces a consistent SQLite backup, reporting SHA-256 and integrity. Use a new destination each time. Preserve source/Compose files with the backup. Python 3 is required on the host; it is not installed in the application container.

On a replacement server, create the named volume before first startup, restore the verified database as `/app/data/coromes.db` with ownership matching the image's unprivileged UID, then start Compose and verify application reads and persistence after restart. Inspect the image UID instead of assuming it remains unchanged. Back up Portainer's data separately while its container is stopped and protect its administrative credentials.

A Hyper-V export of the shut-down application VM is another migration option. Map its adapter to the destination switch and avoid running duplicate host identities. A baseline backup is retained privately; automatic off-host backups remain to be configured.

## Limits and verification

The API has unauthenticated write routes. Its browser login prompt accepts any password. Display configuration routes are placeholders; the viewer's reporting/OEE request returns 404 and falls back to demo data. Builder/admin layouts live in browser localStorage, outside SQLite backups. Export them before changing origins or computers. External Bootstrap assets mean the pages are not fully self-contained offline.

The deployed database was valid but empty. The separate CTI dashboard presents a labeled historical snapshot. A five-minute live collector still needs bounded production-read approval, cost assessment, and authentication setup.

On October 2, 2026, the API build passed with zero warnings/errors after removing unused Swagger dependencies and updating EF Core packages. API/Infrastructure vulnerability checks reported no advisories from current NuGet sources. Docker build, API health, equipment GET, and static page checks passed. Two guest reboots verified container recovery and, after a supervisor fix, automatic tunnel reconnection. SQLite integrity and Portainer login/local-engine checks passed. These checks establish hosting and prototype behavior, not live MES correctness.
