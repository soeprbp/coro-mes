# Current tasks

Updated October 2, 2026.

## Completed for the master prototype

- Portable .NET container with SQLite persistence and loopback access.
- Debian Hyper-V hosting, Docker, and Portainer.
- Source database backup and integrity verification.
- Dependency update, clean API build, and HTTP checks.
- VM reboot and SSH tunnel recovery verification.

## Next work

1. Compare `codex/blazor-forward` with the deployed prototype and prepare a safe migration of the newer application. Preserve its data and check connector defaults before starting it.
2. Prepare a bounded full-cycle CTI read-cost assessment. Prior production approval covered only a completed 20-row pilot, not scheduled reads.
3. Build the five-minute feed through a durable cache with explicit source time, coverage, and stale-data behavior.
4. Configure automatic off-host backups and test a restore.
5. Verify the intended MES board in the physical display rotation when its application and data are ready.

Review the newer branch before rebuilding missing prototype features. Keep the current unauthenticated API on loopback. Private deployment details remain outside Git.
