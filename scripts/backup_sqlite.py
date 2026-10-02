"""Make an integrity-checked SQLite backup for a portable CoroMES move.

Inputs are source and destination database paths. SQLite's online backup API
creates a consistent copy even when the source is open. The command never
modifies the source and refuses to overwrite an existing destination.
"""

import argparse
import hashlib
import json
import sqlite3
from pathlib import Path


def digest(path: Path) -> str:
    """Return the destination's SHA-256 without loading it all into memory."""
    checksum = hashlib.sha256()
    with path.open("rb") as source:
        for chunk in iter(lambda: source.read(1024 * 1024), b""):
            checksum.update(chunk)
    return checksum.hexdigest()


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("source", type=Path)
    parser.add_argument("destination", type=Path)
    args = parser.parse_args()

    source = args.source.resolve(strict=True)
    destination = args.destination.resolve()
    if destination.exists() or source == destination:
        parser.error("destination must be a new path distinct from source")
    destination.parent.mkdir(parents=True, exist_ok=True)

    # URI mode=ro prevents accidental schema or data changes to the source.
    source_db = sqlite3.connect(f"file:{source.as_posix()}?mode=ro", uri=True)
    try:
        target_db = sqlite3.connect(destination)
        try:
            source_db.backup(target_db)
            result = target_db.execute("PRAGMA integrity_check").fetchone()[0]
            if result != "ok":
                raise RuntimeError(f"backup integrity check failed: {result}")
        finally:
            target_db.close()
    finally:
        source_db.close()

    print(json.dumps({
        "source": str(source),
        "destination": str(destination),
        "bytes": destination.stat().st_size,
        "sha256": digest(destination),
        "integrity": "ok",
    }, indent=2))


if __name__ == "__main__":
    main()
