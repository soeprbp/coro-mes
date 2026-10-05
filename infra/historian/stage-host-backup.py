"""Create a verified DB dump and stage only that dump for the SSH backup user.

Run as root with the account name as argv[1]. Credentials are never exported.
The private staging directory is owned by that existing account. Only the
current dump is staged; old staged dumps are retained until explicit cleanup.
"""
import hashlib
import json
import os
from pathlib import Path
import pwd
import shutil
import subprocess
import sys

account = pwd.getpwnam(sys.argv[1])
os.umask(0o077)
subprocess.run(['sh', '/opt/coromes-historian/backup.sh'], check=True, capture_output=True)
source = max(Path('/var/backups/coromes-historian').glob('history-*.dump'), key=lambda p: p.name)
expected = source.with_suffix('.dump.sha256').read_text().split()[0]
stage = Path(account.pw_dir) / '.coromes-backup-export'
if stage.is_symlink():
    raise RuntimeError('Refusing symlink staging directory')
stage.mkdir(mode=0o700, exist_ok=True)
os.chown(stage, account.pw_uid, account.pw_gid)
os.chmod(stage, 0o700)
# Exclusive creation avoids following an existing destination link.
target = stage / source.name
with source.open('rb') as src, target.open('xb') as dst:
    shutil.copyfileobj(src, dst)
with target.open('rb') as staged:
    actual = hashlib.file_digest(staged, 'sha256').hexdigest()
if actual != expected:
    raise RuntimeError('Staged dump checksum mismatch')
os.chown(target, account.pw_uid, account.pw_gid)
print(json.dumps({'path': str(target), 'name': source.name, 'sha256': actual, 'bytes': target.stat().st_size}))
