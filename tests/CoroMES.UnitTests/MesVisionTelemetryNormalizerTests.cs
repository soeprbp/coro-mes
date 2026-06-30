using System.Text.Json;
using CoroMES.Industrial.i3X.Models;
using CoroMES.Integration.IIoT.Services;

namespace CoroMES.UnitTests;

public class MesVisionTelemetryNormalizerTests
{
    [Fact]
    public void Normalize_CreatesCameraZoneReadingsAndEventsFromRocktumblerPayload()
    {
        var collectedAt = new DateTime(2026, 6, 30, 15, 44, 19, DateTimeKind.Utc);
        var normalizer = new MesVisionTelemetryNormalizer();

        var snapshot = normalizer.Normalize(new MesVisionNormalizationInput(
            "MES-Vision",
            "Rocktumbler MES-Vision",
            "https://rocktumbler.57446516.xyz/i3x/v1/",
            "https://rocktumbler.57446516.xyz/",
            collectedAt,
            new ServerInfo { ServerName = "MES-Vision i3X API", ServerVersion = "0.1.0", SpecVersion = "1.0" },
            [
                new ObjectInstance { ElementId = "MES-Vision", DisplayName = "MES-Vision", TypeElementId = "System", IsComposition = true },
                new ObjectInstance { ElementId = "system", DisplayName = "System Health", TypeElementId = "SystemHealth", ParentId = "MES-Vision" },
                new ObjectInstance { ElementId = "events", DisplayName = "Motion Events", TypeElementId = "MotionEvent" },
                new ObjectInstance { ElementId = "camera-0", DisplayName = "Camera USB webcam #0", TypeElementId = "Camera", ParentId = "MES-Vision", IsComposition = true },
                new ObjectInstance { ElementId = "zone-0:rotation", DisplayName = "Zone rotation (0)", TypeElementId = "Zone", ParentId = "camera-0" },
                new ObjectInstance { ElementId = "rotation-0", DisplayName = "Rotation Monitor (USB webcam #0)", TypeElementId = "RotationMonitor", ParentId = "camera-0" }
            ],
            new Dictionary<string, ValueReadResult>
            {
                ["camera-0"] = Value("""{"connected":true,"detectionEnabled":true,"fps":25.12,"framesCaptured":5694561,"framesDropped":0,"lastError":null,"motionLevel":0.0,"source":"0","sourceType":"usb","status":"Monitoring","uptimeSeconds":228717.8}""", collectedAt),
                ["zone-0:rotation"] = Value("""{"color":"#58a6ff","enabled":false,"motionPercent":0.0,"status":"disabled","zoneName":"rotation"}""", collectedAt),
                ["rotation-0"] = Value("""{"lastRotationTime":"2026-06-30T15:44:19.250006+00:00","rpm":42.0,"totalRotations":88732}""", collectedAt),
                ["events"] = Value("""[{"motionPercent":5.79,"region":null,"status":"motion","timestamp":"2026-06-28T10:33:37.677413"},{"motionPercent":1.8,"region":null,"status":"no_motion","timestamp":"2026-06-28T10:33:45.318282"}]""", collectedAt)
            },
            new Dictionary<string, ValueReadResult>()));

        Assert.Equal("MES-Vision", snapshot.Source.ExternalSystemId);
        var camera = Assert.Single(snapshot.Cameras);
        Assert.Equal("camera-0", camera.ElementId);
        Assert.Equal("cam-0", camera.SlotId);
        Assert.Equal("usb", camera.SourceType);
        Assert.Equal("Monitoring", camera.Status);

        var zone = Assert.Single(snapshot.Zones);
        Assert.Equal("zone-0:rotation", zone.ElementId);
        Assert.Equal("camera-0", zone.CameraElementId);
        Assert.Equal("rotation", zone.Name);
        Assert.False(zone.Enabled);

        Assert.Contains(snapshot.Readings, item => item.ElementId == "camera-0" && item.Metric == "fps" && item.NumericValue == 25.12);
        Assert.Contains(snapshot.Readings, item => item.ElementId == "rotation-0" && item.CameraElementId == "camera-0" && item.Metric == "rpm" && item.NumericValue == 42.0);
        Assert.Contains(snapshot.Readings, item => item.ElementId == "zone-0:rotation" && item.CameraElementId == "camera-0" && item.ZoneElementId == "zone-0:rotation" && item.Metric == "enabled" && item.BooleanValue == false);

        Assert.Equal(2, snapshot.Events.Count);
        Assert.Contains(snapshot.Events, item => item.Status == "motion" && item.MotionPercent == 5.79);
        Assert.All(snapshot.Events, item => Assert.Equal(DateTimeKind.Utc, item.OccurredAtUtc.Kind));
    }

    private static ValueReadResult Value(string json, DateTime timestamp)
    {
        return new ValueReadResult
        {
            Quality = "Good",
            Timestamp = timestamp,
            CurrentValue = JsonDocument.Parse(json).RootElement.Clone()
        };
    }
}
