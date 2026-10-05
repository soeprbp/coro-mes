#!/bin/sh
# Root-run logical backup, with partial-file cleanup and atomic publication.
# Keep backups outside the volume. No automatic retention deletion is performed.
set -eu
umask 077
out=${HISTORIAN_BACKUP_DIR:-/var/backups/coromes-historian}
mkdir -p "$out"
stamp=$(date -u +%Y%m%dT%H%M%SZ)
partial="$out/history-$stamp.dump.partial"
trap 'rm -f "$partial"' EXIT HUP INT TERM
docker exec coromes-historian-postgres-1 pg_dump -U postgres -d coromes_history -Fc > "$partial"
test -s "$partial"
docker exec -i coromes-historian-postgres-1 pg_restore --list < "$partial" >/dev/null
mv "$partial" "$out/history-$stamp.dump"
sha256sum "$out/history-$stamp.dump" > "$out/history-$stamp.dump.sha256"
echo "Historian backup saved: $out/history-$stamp.dump"
