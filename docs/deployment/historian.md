# CoroMES historian

The historian is a separate PostgreSQL 17 database named `coromes_history`.
It stores collected source records and their revisions. The existing Blazor
application continues using its own SQLite database; the MES snapshot remains
unchanged until a collector and dashboard adapter are implemented.

## Start and schema

Use `infra/historian/compose.yml` with Compose project `coromes-historian`.
Set `HISTORIAN_ADMIN_SECRET` in a private environment file to the absolute path
of a generated administrator password file. Keep it outside Git. The official
image reads it through a mounted secret. First initialization enables checksums.
The service has a 768 MB memory limit, one CPU quota, and 30 connections.

```sh
docker compose --env-file .env -p coromes-historian -f compose.yml up -d
docker exec -i coromes-historian-postgres-1 psql -X -v ON_ERROR_STOP=1 -U postgres -d coromes_history < schema.sql
docker exec -i coromes-historian-postgres-1 psql -X -v ON_ERROR_STOP=1 -U postgres -d coromes_history < verify.sql
```

The named volume `coromes-historian_historian_data` holds the database. Do not
remove it during updates. The pinned image digest keeps deployments reproducible;
review and deliberately update it for future security releases. Major PostgreSQL
upgrades require a supported data migration, not just changing the image tag.

## Access and ingestion

The database has no host port. Future collector and API containers can join
the internal Docker network `coromes_historian` and connect to `postgres:5432`.
Use distinct login users belonging to `historian_ingest` or `historian_reader`.
Do not give Node-RED or the dashboard the administrator credentials. Keep the
database editor and administration off the public network.

The ingest interface is `historian.record_collection(jsonb)`, returning the run
ID. Its JSON contract and validation rules are documented in `schema.sql`.
Complete batches commit current records and changed versions together. Failed
collections leave current records intact. Repeating unchanged records does not
create duplicate versions. A record is keyed by source, entity type, and source
record ID; explicit deletion flags are retained. Absence from a partial collection
does not mean deletion. Node-RED can produce this contract when conversion is
needed; it is not installed or required by this database layer.

Local observation times use timezone-aware timestamps. Source timestamps retain
their original wall-clock values without assigning a timezone that has not been
verified. This is history of observations, not proof of every intermediate CTI
change between polls. Keep credentials, customer data, and unnecessary source
fields out of incoming payloads.

## Backup and recovery

Install `backup.sh` at `/opt/coromes-historian/backup.sh` and the supplied systemd
service/timer under `/etc/systemd/system`. The timer runs daily at 06:00 UTC with
up to five minutes of jitter. `systemctl enable --now coromes-historian-backup.timer`
enables it. Run `systemctl start coromes-historian-backup.service` for an immediate
backup and inspect `journalctl -u coromes-historian-backup.service` on failure.

Backups use PostgreSQL's consistent logical dump format and are atomically
published under `/var/backups/coromes-historian` with SHA256 sidecars. Failed
partial files are removed. No automatic retention deletion is configured.
Monitor disk usage and copy backups to a protected off-VM location: a local
backup does not protect against losing the VM. Keep private login secrets and
deployment configuration separately; the database dump does not contain roles.

Test restoration into a separately named empty database on a compatible server.
Create the `historian_ingest` and `historian_reader` group roles first if restoring
to a new cluster, then use `pg_restore --exit-on-error --dbname=<test-database>`.
Confirm schema, record/version counts, application privileges, and schema version
before a deliberate cutover. Never overwrite the running database just to test
a backup. Recreate application logins from protected secrets on a new host.

## Operational checks

### Windows VM-host copy

`stage-host-backup.py` creates a fresh guest backup and stages only its dump in
the SSH user's private home directory. `Copy-HistorianBackup.ps1` pulls it with
a pinned SSH host key, verifies SHA256 and size, and publishes it atomically on
the Windows host. Supply the VM hostname, SSH user, private key, known-hosts file,
and destination as parameters. Credentials are not included in the dump.

Run the host script daily with Task Scheduler, start-when-available, bounded
retries, and no overlapping instances. Use a key whose ownership and ACLs satisfy
OpenSSH for the task account; preserve the interactive user's original key.
Restrict the backup folder to administrators and the task account. Inspect
`last-success.json`, `last-failure.json`, and the task exit code; an old failure
file is historical when a newer success exists. No automatic retention deletion
is configured on either machine. A host-local backup survives guest loss but
does not protect against loss of the entire physical host.

Check container health, disk space, the last successful backup, and collection
status. Run `verify.sql` only in a maintenance/test context: it exercises fake
records and rolls its test transaction back. A healthy empty historian does not
mean production collection is running. Collecting from CTI, scheduling reads,
and exposing the data through the dashboard are separate implementation steps.
