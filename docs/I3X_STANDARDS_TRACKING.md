# i3X Standards Tracking

**Last Updated:** 2026-06-18

## Current Upstream Baseline

CoroMES should track CESMII i3X as a moving standard, but the current implementation target is now i3X 1.0 rather than the earlier beta/v0 contract.

| Item | Current Value |
|------|---------------|
| Upstream repository | `https://github.com/cesmii/i3X` |
| Normative branch | `1.0` |
| Latest checked branch commit | `bbaded54e057718916e2ed5d67cd065f691799d3` |
| Latest checked release tag | `1.0.0` |
| Tag commit checked | `34b766442f6ef614d47fe905459a2ea8b91c6f8b` |
| Local upstream inspection copy | external to CoroMES; use a non-OneDrive workspace such as `C:\scripts\external\i3X-upstream` |
| Normative guide | `spec/IMPLEMENTATION_GUIDE.md` |
| Conformance suite | `conformance-tests/` |

## Required Workflow Before i3X Changes

Before changing any CoroMES i3X client, repository adapter, translator, MES-Vision collector, or future CoroMES i3X server code:

1. Fetch upstream:
   ```powershell
   git -C "C:\scripts\external\i3X-upstream" fetch origin 1.0 --depth 1
   git -C "C:\scripts\external\i3X-upstream" switch 1.0
   git -C "C:\scripts\external\i3X-upstream" reset --hard origin/1.0
   git -C "C:\scripts\external\i3X-upstream" rev-parse HEAD
   ```
2. Check upstream release/tag state:
   ```powershell
   git ls-remote --heads --tags https://github.com/cesmii/i3X.git
   ```
3. Review:
   - `README.md`
   - `CHANGELOG.md`
   - `spec/IMPLEMENTATION_GUIDE.md`
   - `spec/UNDERSTANDING_RELATIONSHIPS.md`
   - `conformance-tests/README.md`
4. Record the checked upstream commit in the PR, commit message, or relevant docs.
5. Add or update tests using 1.0-shaped payloads before changing behavior.

## 1.0 Compatibility Deltas That Affect CoroMES

The 1.0 release changes several assumptions from earlier beta notes and from the current CoroMES i3X client surface:

- `GET /objects` filters by `typeElementId`, not `typeId`.
- `POST /objects/history` is a required query endpoint. Servers without historical data should return `GoodNoData` rather than pretending the endpoint does not exist.
- `PUT /objects/{elementId}/value` and `PUT /objects/{elementId}/history` were replaced by bulk `PUT /objects/value` and `PUT /objects/history`.
- Subscription management is body-oriented:
  - `POST /subscriptions`
  - `POST /subscriptions/list`
  - `POST /subscriptions/delete`
  - `POST /subscriptions/register`
  - `POST /subscriptions/unregister`
  - `POST /subscriptions/sync`
  - `POST /subscriptions/stream`
- Subscription requests require `clientId` in the request body.
- `/subscriptions/sync` returns batches shaped like `[{ "sequenceNumber": N, "updates": [...] }]`.
- `lastSequenceNumber=-1` clears pending updates in a sync call.
- Streaming is optional. CoroMES collectors must keep sync/polling as the baseline path.
- Timestamps must be UTC with `Z` and no timezone offset.
- Error payloads use `responseDetail`.

## CoroMES Client Alignment

`CoroMES.Industrial.i3X.I3XClient` now supports the main CESMII 1.0 route shapes needed before MES-Vision collection:

- `GetObjectsAsync` sends `typeElementId` when filtering objects.
- `ObjectInstance` still exposes `TypeId` for CoroMES compatibility, while JSON reads/writes use `typeElementId` and can still read legacy `typeId`.
- current-value writes use bulk `PUT /objects/value` with an `updates` array.
- history writes use bulk `PUT /objects/history` with an `updates` array.
- subscription create/list/delete/register/unregister/sync calls use body-oriented routes with `clientId` and `subscriptionId`.
- `I3XOptions.ClientId` provides the default CoroMES subscription client identity for future collector wiring.

## Remaining CoroMES Implementation Implications

- Keep read-only value/history polling as the first MES-Vision collector path.
- Add a higher-level collector service that owns `clientId`, subscription lifecycle, sync acknowledgements, retry/backoff, and raw payload capture.
- Treat SSE streaming as optional even when an endpoint advertises i3X support.
- Normalize incoming timestamps to UTC and preserve original payload JSON for traceability.
- Add compatibility tests against the running MES-Vision i3X endpoint once its target-side update settles.
- Run the upstream conformance suite against any future CoroMES i3X server endpoint.

## MES-Vision Note

`soeprbp/mes-vision` is useful as a practical test endpoint, but it may lag or lead the official specification. CoroMES should maintain a thin compatibility layer for MES-Vision quirks while keeping the core i3X client aligned to CESMII 1.0.
