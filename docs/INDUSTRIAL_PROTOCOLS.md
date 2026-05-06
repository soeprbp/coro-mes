# Industrial Protocols

CoroMES supports three industrial protocols out of the box for connecting to shop floor devices.

---

## MQTT

### Overview
MQTT (Message Queuing Telemetry Transport) is a lightweight pub/sub protocol ideal for IoT sensors and edge devices.

### Architecture
```
┌─────────────┐      MQTT       ┌──────────────┐
│   Sensors   │ ──────────────▶│   Mosquitto  │
│   Edge      │                 │    Broker    │
│   Devices   │                 └──────┬───────┘
└─────────────┘                        │ MQTT
                                        ▼
                               ┌──────────────┐
                               │    CoroMES   │
                               │ MQTT Client  │
                               └──────┬───────┘
                                      ▼
                               ┌──────────────┐
                               │  PostgreSQL │
                               └──────────────┘
```

### Configuration
```yaml
industrial:
  mqtt:
    enabled: true
    mode: "external"  # or "embedded" for built-in broker
    brokerHost: "localhost"
    brokerPort: 1883
    username: ""
    password: ""
    clientId: "coromes-mqtt-client"
    defaultQoS: 1
    topics:
      - "plant/+/+/status"
      - "plant/+/+/telemetry"
      - "plant/+/+/alerts"
```

### Topic Structure
```
plant/{floor}/{line}/temperature
plant/{floor}/{line}/pressure
plant/{floor}/{line}/status
plant/machines/{machineId}/production/count
plant/machines/{machineId}/alarm
plant/alerts/{severity}/{source}
```

### API Endpoints
```csharp
// Publish message
POST /industrial/v1/mqtt/publish
{
  "topic": "plant/line01/command",
  "payload": "{\"action\": \"start\"}",
  "qos": 1,
  "retain": false
}

// Subscribe to topic
POST /industrial/v1/mqtt/subscribe
{
  "topic": "plant/+/+/status",
  "qos": 1
}

// Get connection status
GET /industrial/v1/mqtt/status
```

### Message Format
```json
{
  "timestamp": "2026-04-16T10:30:00Z",
  "topic": "plant/line01/temperature",
  "payload": {
    "value": 72.5,
    "unit": "F",
    "sensorId": "TEMP-001"
  }
}
```

---

## OPC-UA

### Overview
OPC-UA (Open Platform Communications Unified Architecture) is a robust, secure protocol for industrial automation. Used for SCADA systems, Siemens PLCs, and HMI integration.

### Configuration
```yaml
industrial:
  opcua:
    enabled: true
    servers:
      - name: "SCADA_Server"
        url: "opc.tcp://scada.local:4840"
        securityPolicy: "Basic256Sha256"
        authMode: "Anonymous"
      - name: "Siemens_PLC"
        url: "opc.tcp://plc-siemens.local:4840"
        securityPolicy: "Basic128Rsa15"
        authMode: "UserName"
        username: "admin"
        password: "${OPCUA_PASSWORD}"
```

### Common Tag Patterns
```
MachineStatus (Int16)
ProductionCount (Int32)
CycleTime (Double)
Temperature (Double)
AlarmCode (Int16)
PartCount_Good (Int32)
PartCount_Scrap (Int32)
```

### API Endpoints
```csharp
// List available servers
GET /industrial/v1/opcua/servers

// Browse server nodes
GET /industrial/v1/opcua/{server}/browse?nodeId=root

// Read tags
POST /industrial/v1/opcua/{server}/read
{
  "nodes": [
    "ns=2;s=MachineStatus",
    "ns=2;s=ProductionCount"
  ]
}

// Write tag
POST /industrial/v1/opcua/{server}/write
{
  "nodeId": "ns=2:s=StartCommand",
  "value": true,
  "type": "Boolean"
}

// Subscribe to changes
POST /industrial/v1/opcua/{server}/subscribe
{
  "nodes": ["ns=2:s=MachineStatus"],
  "interval": 1000  // ms
}
```

### Reading Response
```json
{
  "results": [
    {
      "nodeId": "ns=2;s=MachineStatus",
      "value": 1,
      "timestamp": "2026-04-16T10:30:00Z",
      "quality": "Good"
    }
  ]
}
```

---

## Ethernet/IP

### Overview
Ethernet/IP (EIP) is a CIP-based protocol primarily used by Allen-Bradley/Rockwell PLCs. Enables reading/writing tags directly from ControlLogix, CompactLogix, and Micro800 series.

### Configuration
```yaml
industrial:
  ethernetip:
    enabled: true
    devices:
      - name: "AB_PLC_01"
        host: "192.168.1.10"
        port: 44818
        slot: 0  # Processor slot (0 = backplane)
        tags:
          - name: "MachineStatus"
            address: "MainProgram:MachineStatus"
            type: "Int16"
          - name: "PartCount"
            address: "MainProgram:PartCount"
            type: "Int32"
          - name: "CycleTime"
            address: "MainProgram:CycleTime"
            type: "Real"
      - name: "AB_PLC_02"
        host: "192.168.1.11"
        port: 44818
        slot: 0
```

### Tag Syntax (Allen-Bradley)
| Address Type | Example | Description |
|--------------|---------|-------------|
| Controller Tags | `MainProgram:PartCount` | User-defined tags |
| Message | `MyMessage.MESSAGEDATA[0]` | Array elements |
| Timer | `MyTimer.PRE` | Timer preset |
| Counter | `MyCounter.ACC` | Counter accumulator |

### Data Types
| Type | Size | .NET Equivalent |
|------|------|-----------------|
| BOOL | 1 bit | bool |
| SINT | 8 bit | sbyte |
| INT | 16 bit | short |
| DINT | 32 bit | int |
| REAL | 32 bit | float |
| LREAL | 64 bit | double |
| STRING | variable | string |

### API Endpoints
```csharp
// Read tags
POST /industrial/v1/ethernetip/{device}/read
{
  "tags": ["MainProgram:MachineStatus", "MainProgram:PartCount"]
}

// Read response
{
  "results": [
    { "tag": "MainProgram:MachineStatus", "value": 1, "type": "Int16" },
    { "tag": "MainProgram:PartCount", "value": 1234, "type": "Int32" }
  ]
}

// Write tag
POST /industrial/v1/ethernetip/{device}/write
{
  "tag": "MainProgram:StartCommand",
  "value": true,
  "type": "BOOL"
}

// Bulk read (recommended for performance)
POST /industrial/v1/ethernetip/{device}/readBulk
{
  "tags": [
    { "name": "Status", "address": "MainProgram:MachineStatus" },
    { "name": "Count", "address": "MainProgram:PartCount" }
  ]
}
```

### Connecting to PLC

1. Ensure network connectivity (same subnet or routed)
2. Configure device IP and port (default 44818)
3. Add tags to configuration
4. Test connection with browse endpoint
5. Map tags to MES entities in tagMapping config

---

## Tag Mapping Configuration

Connect industrial tags to MES entities:

```yaml
tagMapping:
  # MQTT → MES
  - industrialTag: "LINE01.Status"
    mesEntity: "ProductionLine"
    mesProperty: "Status"
    protocol: "mqtt"
    topic: "plant/line01/status"

  # OPC-UA → MES
  - industrialTag: "PLC01.ProductionCount"
    mesEntity: "WorkOrder"
    mesProperty: "CompletedQuantity"
    protocol: "opcua"
    server: "SCADA_Server"
    nodeId: "ns=2;s=ProductionCount"

  # Ethernet/IP → MES
  - industrialTag: "AB_PLC_01.PartCount"
    mesEntity: "WorkOrder"
    mesProperty: "CompletedQuantity"
    protocol: "ethernetip"
    device: "AB_PLC_01"
    address: "MainProgram:PartCount"
```

---

## Data Bridge

The data bridge maps incoming industrial data to MES entities:

```
Industrial Data → Tag Manager → Data Bridge → PostgreSQL
                       │              │
                       │              ▼
                       │      ┌────────────────┐
                       │      │  Business      │
                       │      │  Rules Engine  │
                       │      └────────────────┘
                       ▼
              ┌────────────────┐
              │  Time-Series   │
              │    Storage     │
              └────────────────┘
```

### Events Generated
- `WorkOrderStarted` - When production starts
- `WorkOrderCompleted` - When quantity reached
- `EquipmentStatusChanged` - Status update
- `QualityAlarm` - Out of tolerance
- `DowntimeStarted` - Machine stopped
- `DowntimeEnded` - Machine resumed