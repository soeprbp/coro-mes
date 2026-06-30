# MES-Vision Integration Plan

**Last Updated:** 2026-06-30

## Purpose

`soeprbp/mes-vision` will be the first vision-based testing endpoint for CoroMES dashboards, reports, and industrial telemetry ingestion. Treat MES-Vision as an edge device that watches equipment or HMI screens, derives machine state from video, and exposes that state through REST, i3X, SocketIO, and local JSONL history.

## Source Repository

- Repository: `soeprbp/mes-vision`
- Visibility: private
- Local investigation copy: external to CoroMES; use a non-OneDrive workspace such as `C:\scripts\external\mes-vision`
- Current branch inspected: `master`
- Current head inspected: `a3aead6`

## What MES-Vision Provides

### Runtime Services

| Service | Default URL | Purpose |
|---------|-------------|---------|
| Dashboard | `http://localhost:5000` | Flask UI, live MJPEG stream, OEE page, history page, config page |
| Dashboard API | `http://localhost:5000/api/*` | Local JSON control/status/history endpoints |
| i3X API | `http://localhost:5002/v1/*` | CESMII-style object/value/history/subscription API |
| i3X proxy | `http://localhost:5000/i3x/*` | Proxy through the dashboard host for tunnels |
| SocketIO | dashboard host | Live status and motion events for browser clients |

Current remote test endpoint:

- Dashboard: `https://rocktumbler.57446516.xyz/`
- i3X API through dashboard proxy: `https://rocktumbler.57446516.xyz/i3x/v1/`

### Data Domains

MES-Vision can provide CoroMES with:

- camera inventory: slot id, camera source, source type, source description, active slot
- camera health: connected, available, FPS, frames captured, frames dropped, uptime, last error
- detection state: enabled/disabled, current status, motion level
- zones: name, enabled flag, color, status, motion percent, source/slot ownership
- state-change events: timestamp, source, status, motion percent, optional zone name
- i3X object metadata: `Camera`, `Zone`, `MotionEvent`, `SystemHealth`, `System`
- i3X current values and history values for cameras, zones, system health, and events

### Important Payload Shapes

Dashboard status:

```json
{
  "version": "0.4.0-beta.1",
  "camera_connected": true,
  "motion_detection_enabled": true,
  "status": "RUNNING",
  "motion_level": 12.5,
  "slot_id": "cam-0"
}
```

Camera info:

```json
{
  "connected": true,
  "available": true,
  "source": "0",
  "source_type": "usb",
  "source_description": "USB Camera 0",
  "fps": 30.0,
  "frames_captured": 12345,
  "frames_dropped": 12,
  "uptime_seconds": 3600,
  "last_error": null,
  "zones": [],
  "detection_enabled": true,
  "motion_level": 7.4,
  "status": "RUNNING",
  "slot_id": "cam-0"
}
```

Historian event:

```json
{
  "timestamp": "2026-06-10T16:30:00.000000+00:00",
  "motion_percent": 12.5,
  "status": "RUNNING",
  "zone_name": "feed-end",
  "source": "0"
}
```

i3X camera value:

```json
{
  "isComposition": true,
  "value": {
    "source": "0",
    "sourceType": "usb",
    "connected": true,
    "fps": 30.0,
    "framesCaptured": 12345,
    "framesDropped": 12,
    "uptimeSeconds": 3600,
    "detectionEnabled": true,
    "motionLevel": 7.4,
    "status": "RUNNING",
    "lastError": null
  },
  "quality": "Good",
  "timestamp": "2026-06-10T16:30:00.000000Z"
}
```

## Recommended Collection Strategy

### Phase 1: Poll Through i3X

Use CoroMES' existing `CoroMES.Industrial.i3X` client as the preferred integration path, but update it against the official CESMII i3X 1.0 contract before treating MES-Vision as authoritative.

1. Call `/v1/info` to verify the endpoint is healthy and supports history/subscription capability.
2. Call `/v1/objects?includeMetadata=true` to discover cameras and zones.
3. Poll `/v1/objects/value` for:
   - `system`
   - every `camera-*`
   - every `zone-*`
   - optionally `events`
4. Poll `/v1/objects/history` for `events` on a rolling interval to backfill missed state changes.
5. Store normalized readings in CoroMES tables before building dashboards on top.

This is the simplest first connector because it is HTTP-only, testable, and fits the industrial/i3X direction already present in CoroMES.

### Phase 2: Subscribe Through i3X SSE

After polling works, add an SSE listener for `/v1/subscriptions/{subscriptionId}/stream` and use it for lower-latency updates. Keep the polling collector as a backstop so CoroMES can recover from dropped SSE connections.

### Phase 3: Optional Dashboard API Fallback

Use the dashboard API only as a fallback or troubleshooting path:

- `/api/status`
- `/api/camera/info`
- `/api/slots`
- `/api/zones`
- `/api/historian/query`
- `/api/historian/stats`

The dashboard API is useful, but i3X gives CoroMES a more durable contract for future industrial endpoints.

## CoroMES Storage Model

Add a vision telemetry boundary rather than storing raw frames in the MES database.

### Source Registry

`VisionSources`

| Field | Purpose |
|-------|---------|
| `Id` | CoroMES source id |
| `EndpointBaseUrl` | i3X or dashboard base URL |
| `ExternalSystemId` | stable endpoint id, such as `MES-Vision` |
| `DisplayName` | human label |
| `EquipmentId` | optional CoroMES equipment match |
| `IsActive` | source enabled flag |
| `LastSeenAtUtc` | last successful contact |
| `LastError` | most recent collector error |

### Camera And Zone Inventory

`VisionCameras`

| Field | Purpose |
|-------|---------|
| `VisionSourceId` | parent source |
| `ElementId` | i3X camera id, such as `camera-0` |
| `SlotId` | MES-Vision slot id |
| `Source` | camera source string |
| `SourceType` | usb/rtsp/http/hls/url |
| `EquipmentId` | optional equipment mapping |

`VisionZones`

| Field | Purpose |
|-------|---------|
| `VisionCameraId` | parent camera |
| `ElementId` | i3X zone id |
| `Name` | zone name |
| `EquipmentId` | optional equipment mapping |
| `Enabled` | last known enabled state |

### Time-Series History

`VisionReadings`

| Field | Purpose |
|-------|---------|
| `VisionSourceId` | parent source |
| `VisionCameraId` | optional camera |
| `VisionZoneId` | optional zone |
| `ElementId` | i3X element id |
| `OccurredAtUtc` | timestamp from source |
| `CollectedAtUtc` | ingestion timestamp |
| `Metric` | `motionLevel`, `status`, `connected`, `fps`, `framesDropped`, etc. |
| `NumericValue` | numeric metric value |
| `TextValue` | state/string metric value |
| `BooleanValue` | boolean metric value |
| `Quality` | i3X quality |
| `RawJson` | compact source payload for traceability |

`VisionEvents`

| Field | Purpose |
|-------|---------|
| `VisionSourceId` | parent source |
| `VisionCameraId` | optional camera |
| `VisionZoneId` | optional zone |
| `OccurredAtUtc` | event timestamp |
| `Source` | MES-Vision source string |
| `Status` | `RUNNING`, `IDLE`, `motion`, `no_motion`, etc. |
| `MotionPercent` | event intensity |
| `RawJson` | original event payload |

Keep image/frame retention out of the first implementation. If snapshots are needed later, store file/object references only, not frame blobs in relational tables.

## Dashboard And Reporting Uses

### Operational Dashboards

- camera health tile: connected, last seen, FPS, dropped frames
- equipment state tile: running/idle/no signal from latest vision status
- zone activity board: motion percent and zone status by equipment area
- event timeline: state changes over shift/day
- exception list: disconnected cameras, high dropped frames, stale source, no recent events

### Reporting

- runtime from vision-derived `RUNNING` intervals
- idle time from vision-derived `IDLE` intervals
- state-change count by equipment/zone/shift
- camera reliability: uptime, dropped frame rate, last error frequency
- correlation with CoroMES work orders once cameras are mapped to equipment

## Compatibility Notes

The official CESMII i3X baseline is tracked in `docs/I3X_STANDARDS_TRACKING.md`. The investigated MES-Vision code exposes a strong practical test surface, but CoroMES should handle these differences explicitly:

- MES-Vision i3X default port is currently `5002` in `config/config.yaml`, while some docs mention `5001`.
- Official i3X 1.0 and MES-Vision object filtering use `typeElementId`; the current CoroMES i3X client sends `typeId`.
- MES-Vision history requires `startTime` and `endTime`; CoroMES should always provide both.
- MES-Vision implements subscription sync as `POST /v1/subscriptions/sync` with `subscriptionId` in the body. The CoroMES i3X client now uses this body-oriented 1.0 route shape.
- Official i3X 1.0 and MES-Vision implement bulk value writes at `PUT /v1/objects/value`; current CoroMES write code uses per-object `PUT /v1/objects/{id}/value`.
- Official i3X 1.0 requires subscription `clientId` scoping and body-oriented subscription routes; CoroMES does not yet model that.
- MES-Vision SSE stream is implemented, but CoroMES does not yet have a streaming i3X client method. Official i3X 1.0 treats streaming as optional, so polling/sync stays the baseline.

For the first collector, prefer read-only discovery, value polling, and history polling. The core CoroMES i3X client now uses the main CESMII 1.0 route shapes, so add a MES-Vision-specific compatibility shim only where MES-Vision differs from the standard.

## Security Notes

- Do not expose camera stream URLs, RTSP credentials, or tunnel URLs in committed config.
- Treat dashboard control endpoints as privileged because they can switch sources, calibrate, stop detection, edit zones, and change config.
- The first CoroMES collector should use read-only i3X calls.
- If remote tunnels are used, prefer authenticated/private access; do not assume `trycloudflare` or bore URLs are safe for plant data.
- Audit any future CoroMES write commands that calibrate, toggle detection, switch source, or change zones.

## Firefly Team Assignments

- Mal: keep MES-Vision integration scoped as the first industrial telemetry test endpoint.
- River: verify real payloads from a running MES-Vision instance and compare them to this document.
- Wash: build the read-only collector, storage migration, and Blazor dashboard widgets.
- Kaylee: keep memory files and dashboard/reporting checklists current as telemetry work advances.
- Jane: review endpoint exposure, tunnel usage, credentials, and any future write/control path.

## Current CoroMES Implementation

- `integration/CoroMES.Integration.IIoT` contains a read-only MES-Vision i3X collector.
- `MesVisionCollector` config points at `https://rocktumbler.57446516.xyz/i3x/v1/` by default.
- Background polling is disabled by default; manual collection is available through `POST /api/v1/integration/mes-vision/collect`.
- The collector polls `/info`, `/objects`, `/objects/value`, and `/objects/history`.
- EF persistence now includes `VisionSources`, `VisionCameras`, `VisionZones`, `VisionReadings`, and `VisionEvents`.
- Admin-protected endpoints expose sources, mapping rows, recent readings, and recent events under `/api/v1/integration/mes-vision/*`.
- `/admin/vision` maps collected cameras and zones to CoroMES equipment.
- Camera and zone equipment mapping updates validate equipment ids, support clearing the mapping, and write `VisionCamera`/`VisionZone` audit records.
- Unit coverage normalizes captured Rocktumbler-style i3X camera, zone, rotation, and event payloads.
- Integration coverage verifies camera/zone mapping API behavior and audit logging.

## Next Implementation Steps

1. Add dashboard cards and reporting queries from normalized `VisionReadings` and `VisionEvents`, grouped through the camera/zone equipment mappings.
2. Add optional live compatibility tests against the running Rocktumbler i3X endpoint without making the normal test suite network-dependent.
3. Decide whether MES-Vision collector configuration should be user-editable at runtime or deployment-only.
4. Add SSE subscription support only after polling is stable and recoverable.
