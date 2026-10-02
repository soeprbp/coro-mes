# CoroMES

CoroMES is a .NET manufacturing execution system under development.

## Branches and deployed version

- `codex/blazor-forward` contains the newer Blazor application, persisted displays, and integration work. Check it before planning product features.
- This master-based deployment packages the older Minimal API and static web prototype in a portable Debian/Docker host. It does not include the newer Blazor branch.

The prototype uses SQLite in a persistent Docker volume. Its interface has placeholder authentication and demo display behavior, so the API is restricted to loopback with SSH access. A separate CTI board shows a labeled historical snapshot; live collection remains pending.

## Run the portable prototype

With Docker Engine and Compose installed:

```bash
docker compose -f deploy/portable/compose.yaml up -d --build
curl --fail http://127.0.0.1:5100/health
```

Open `http://127.0.0.1:5100/admin/index.html` on the host or forward that port over SSH. Do not expose the prototype's write API directly to the network. The older root compose file is a development sketch, not the current deployment entry point.

## Documentation

- [Portable deployment, backup, and recovery](docs/PORTABLE_DEPLOYMENT.md)
- [Documentation index](docs/INDEX.md)
- [API](docs/API.md)
- [Project state](memory/PROJECT_STATE.md)
- [Next tasks](memory/CURRENT_TASKS.md)

Private deployment addresses and credentials belong outside Git. The local operations pointer is `docs/DEPLOYMENT.local.md`, which is ignored.
