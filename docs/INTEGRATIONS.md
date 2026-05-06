# Integrations

CoroMES integrates with external systems for EDI, CMMS, and Industrial IoT.

---

## TrueCommerce (EDI)

### Overview
TrueCommerce provides EDI (Electronic Data Interchange) services for exchanging business documents with trading partners.

### Supported Transactions
| Transaction | Code | Direction | Description |
|-------------|------|-----------|-------------|
| Purchase Order | 850 | Inbound | Customer orders |
| Invoice | 810 | Outbound | Customer billing |
| ASN | 856 | Outbound | Advance ship notice |
| Functional Ack | 997 | Both | Transaction receipt |

### Configuration

```yaml
truecommerce:
  enabled: true
  partnerId: "YOUR_PARTNER_ID"
  connectionType: "AS2"
  inboundFolder: "./data/edi/inbound"
  outboundFolder: "./data/edi/outbound"
  archiveFolder: "./data/edi/archive"
```

### API Endpoints

```csharp
// Process incoming EDI
POST /api/v1/integration/edi/process
{
  "filePath": "./data/edi/inbound/PO_12345.x12"
}

// Get EDI status
GET /api/v1/integration/edi/{id}

// Generate outbound invoice
POST /api/v1/integration/edi/invoice
{
  "workOrderId": 123,
  "customerId": "CUST001"
}
```

### Processing Flow
```
Inbound X12 File
    │
    ▼
┌─────────────┐
│   Parser    │  Parse 850/856
└──────┬──────┘
       │
       ▼
┌─────────────┐
│  Validator  │  Validate structure
└──────┬──────┘
       │
       ▼
┌─────────────┐
│   Mapper    │  Map to WorkOrder
└──────┬──────┘
       │
       ▼
┌─────────────┐
│  Database   │  Store order
└─────────────┘
```

---

## Upkeep.com (CMMS)

### Overview
Upkeep is a cloud-based Computerized Maintenance Management System (CMMS) for tracking maintenance work orders, assets, and schedules.

### Features
- Asset management
- Work order creation and tracking
- Preventive maintenance scheduling
- Parts/inventory management
- User management

### Configuration

```yaml
upkeep:
  enabled: true
  apiUrl: "https://api.upkeep.com"
  apiKey: "${UPKEEP_API_KEY}"
  syncInterval: 300
  pullWorkOrders: true
  pushEquipment: true
```

### API Endpoints

```csharp
// Sync equipment
POST /api/v1/integration/upkeep/sync/equipment

// Get work orders
GET /api/v1/integration/upkeep/workorders

// Create maintenance work order
POST /api/v1/integration/upkeep/workorder
{
  "equipmentId": "EQ001",
  "type": "Preventive",
  "description": "Replace filter",
  "scheduledDate": "2026-04-20"
}

// Link production downtime to maintenance
POST /api/v1/integration/upkeep/downtime
{
  "equipmentId": "EQ001",
  "startTime": "2026-04-15T10:00:00Z",
  "endTime": "2026-04-15T12:00:00Z",
  "cause": "Bearing failure"
}
```

### Sync Behavior

| Direction | Trigger | Data |
|-----------|---------|------|
| Upkeep → CoroMES | Every 5 min (configurable) | Work orders, assets |
| CoroMES → Upkeep | On equipment change | New/updated equipment |

---

## IIoT Gateway

### Overview
The Industrial IoT (IIoT) gateway provides unified access to industrial devices and protocols.

### Architecture
```
┌─────────────────────────────────────────┐
│             IIoT Gateway                │
│                                         │
│  ┌─────────┐  ┌─────────┐  ┌─────────┐ │
│  │  MQTT   │  │ OPC-UA  │  │  EIP   │ │
│  │ Client  │  │ Client  │  │ Client │ │
│  └─────────┘  └─────────┘  └─────────┘ │
│         │         │         │          │
│         └─────────┼─────────┘          │
│                   ▼                    │
│         ┌─────────────────┐            │
│         │  Tag Manager   │            │
│         └────────┬────────┘            │
│                  ▼                     │
│         ┌─────────────────┐            │
│         │  Data Bridge   │            │
│         └────────┬────────┘            │
└──────────────────┼──────────────────────┘
                   ▼
          ┌────────────────┐
          │  PostgreSQL    │
          └────────────────┘
```

### Configuration

```yaml
industrial:
  enabled: true
  mqtt:
    enabled: true
    brokerHost: "localhost"
    brokerPort: 1883
  opcua:
    enabled: false
    servers:
      - name: "SCADA"
        url: "opc.tcp://scada.local:4840"
  ethernetip:
    enabled: false
    devices:
      - name: "AB_PLC_01"
        host: "192.168.1.10"
```

### Tag Mapping

Map industrial tags to MES entities:

```yaml
tagMapping:
  - industrialTag: "LINE01_Status"
    mesEntity: "ProductionLine"
    mesProperty: "Status"
    protocol: "mqtt"
    topic: "plant/line01/status"
```

---

## Adding New Integrations

### Step 1: Create Project
```powershell
dotnet new classlib -n CoroMES.Integration.NewSystem
```

### Step 2: Add to Solution
```powershell
dotnet sln add integration/CoroMES.Integration.NewSystem/...
```

### Step 3: Implement Interface

```csharp
// IIntegrationService.cs
public interface IIntegrationService
{
    Task<bool> ConnectAsync();
    Task<IntegrationResult> SyncAsync(SyncRequest request);
    Task DisconnectAsync();
}
```

### Step 4: Register in DI
```csharp
// Program.cs
builder.Services.AddScoped<IIntegrationService, NewSystemService>();
```

### Step 5: Add Endpoint
```csharp
app.MapPost("/api/v1/integration/new/sync", 
    async (IIntegrationService service, CancellationToken ct) =>
{
    var result = await service.SyncAsync(new SyncRequest());
    return Results.Ok(result);
});
```