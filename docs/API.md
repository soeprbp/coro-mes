# API Overview

CoroMES provides multiple APIs for different use cases:

| API | Base Path | Purpose | Consumers |
|-----|-----------|---------|-----------|
| Core | `/api/v1` | CRUD operations | Shop floor apps, operators |
| Reporting | `/reporting/v1` | Analytics/aggregation | Power BI, Tableau, Excel |
| Industrial | `/industrial/v1` | Protocol management | SCADA, PLCs, sensors |

## Core API Endpoints

### Work Orders
```
GET    /api/v1/workorders           # List all work orders
POST   /api/v1/workorders           # Create work order
GET    /api/v1/workorders/{id}     # Get work order by ID
PUT    /api/v1/workorders/{id}     # Update work order
DELETE /api/v1/workorders/{id}     # Delete work order
POST   /api/v1/workorders/{id}/start   # Start production
POST   /api/v1/workorders/{id}/complete # Complete production
POST   /api/v1/workorders/{id}/scrap    # Record scrap
```

### Equipment
```
GET    /api/v1/equipment            # List equipment
POST   /api/v1/equipment            # Register equipment
GET    /api/v1/equipment/{id}       # Get equipment details
PUT    /api/v1/equipment/{id}       # Update equipment
DELETE /api/v1/equipment/{id}       # Remove equipment
GET    /api/v1/equipment/{id}/status   # Get current status
POST   /api/v1/equipment/{id}/maintenance # Schedule maintenance
```

### Inventory
```
GET    /api/v1/inventory             # List inventory
POST   /api/v1/inventory             # Add material
GET    /api/v1/inventory/{id}       # Get material details
PUT    /api/v1/inventory/{id}       # Update material
POST   /api/v1/inventory/movement   # Record material movement
GET    /api/v1/inventory/bom/{productId} # Get BOM
```

### Quality
```
GET    /api/v1/quality/inspections   # List inspections
POST   /api/v1/quality/inspections   # Create inspection
GET    /api/v1/quality/inspections/{id} # Get inspection
POST   /api/v1/quality/inspections/{id}/result # Record result
GET    /api/v1/quality/ncr           # List non-conformances
POST   /api/v1/quality/ncr           # Create NCR
```

### Workforce
```
GET    /api/v1/workforce/shifts      # List shifts
POST   /api/v1/workforce/shifts      # Create shift
GET    /api/v1/workforce/operators   # List operators
POST   /api/v1/workforce/operators   # Register operator
POST   /api/v1/workforce/labor       # Record labor
```

### Integrations
```
POST   /api/v1/integration/edi/process    # Process EDI file
POST   /api/v1/integration/edi/ack       # Send EDI acknowledgment
POST   /api/v1/integration/upkeep/sync   # Sync with Upkeep
GET    /api/v1/integration/upkeep/status # Get sync status
```

---

## Reporting API Endpoints

### OEE Metrics
```
GET /reporting/v1/oee
GET /reporting/v1/oee/trend?from=2026-01-01&to=2026-04-15&granularity=day
```

### Production Reports
```
GET /reporting/v1/production/daily
GET /reporting/v1/production/by-line
GET /reporting/v1/production/by-operator
GET /reporting/v1/production/schedule-compliance
```

### Quality Reports
```
GET /reporting/v1/quality/inspections?from=2026-01-01&to=2026-04-15
GET /reporting/v1/quality/ncr
GET /reporting/v1/quality/ trends
```

### Equipment Reports
```
GET /reporting/v1/equipment/oee
GET /reporting/v1/equipment/downtime
GET /reporting/v1/equipment/utilization
```

### Inventory Reports
```
GET /reporting/v1/inventory/current
GET /reporting/v1/inventory/movements
GET /reporting/v1/inventory/valuation
```

### Labor Reports
```
GET /reporting/v1/labor/hours
GET /reporting/v1/labor/productivity
```

---

## Industrial API Endpoints

### MQTT
```
POST /industrial/v1/mqtt/publish
GET  /industrial/v1/mqtt/subscribe
GET  /industrial/v1/mqtt/status
```

### OPC-UA
```
GET  /industrial/v1/opcua/servers
GET  /industrial/v1/opcua/{server}/browse
POST /industrial/v1/opcua/{server}/read
POST /industrial/v1/opcua/{server}/write
GET  /industrial/v1/opcua/{server}/subscribe
```

### Ethernet/IP
```
POST /industrial/v1/ethernetip/{device}/read
POST /industrial/v1/ethernetip/{device}/write
GET  /industrial/v1/ethernetip/{device}/tags
```

---

## Query Parameters

All reporting endpoints support:

| Parameter | Description | Example |
|-----------|-------------|---------|
| `from` | Start date | `?from=2026-01-01` |
| `to` | End date | `?to=2026-04-15` |
| `line` | Filter by line | `?line=LINE01` |
| `shift` | Filter by shift | `?shift=DAY` |
| `operator` | Filter by operator | `?operator=OP001` |
| `granularity` | Time grouping | `?granularity=day` |
| `format` | Output format | `?format=csv` |
| `page` | Page number | `?page=1` |
| `pageSize` | Page size | `?pageSize=100` |

---

## Response Formats

### JSON (Default)
```json
{
  "data": [...],
  "pagination": {
    "page": 1,
    "pageSize": 100,
    "totalItems": 500,
    "totalPages": 5
  }
}
```

### CSV Export
```
GET /reporting/v1/production/daily?format=csv
```

Returns CSV with headers for Excel import.

---

## Versioning

API uses URL-based versioning:
- Core: `/api/v1/*`
- Reporting: `/reporting/v1/*`
- Industrial: `/industrial/v1/*`

Header-based version negotiation (optional):
```
API-Version: 2026-04-16
Accept: application/json; version=1
```

---

## Authentication

Currently:
- No authentication (development mode)
- Future: API key or OAuth2

## Rate Limiting

Not implemented yet. Planned:
- 100 requests/minute for Core API
- 60 requests/minute for Reporting API