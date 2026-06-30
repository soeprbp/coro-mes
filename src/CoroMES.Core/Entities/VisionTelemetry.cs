namespace CoroMES.Core.Entities;

public class VisionSource : Entity
{
    public string ExternalSystemId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string EndpointBaseUrl { get; set; } = string.Empty;
    public string? DashboardBaseUrl { get; set; }
    public int? EquipmentId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastSeenAtUtc { get; set; }
    public DateTime? LastCollectedAtUtc { get; set; }
    public string? LastError { get; set; }

    public Equipment? Equipment { get; set; }
    public ICollection<VisionCamera> Cameras { get; set; } = new List<VisionCamera>();
    public ICollection<VisionZone> Zones { get; set; } = new List<VisionZone>();
}

public class VisionCamera : Entity
{
    public int VisionSourceId { get; set; }
    public string ElementId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? SlotId { get; set; }
    public string? Source { get; set; }
    public string? SourceType { get; set; }
    public int? EquipmentId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastSeenAtUtc { get; set; }
    public string? LastStatus { get; set; }
    public double? LastMotionLevel { get; set; }

    public VisionSource? VisionSource { get; set; }
    public Equipment? Equipment { get; set; }
    public ICollection<VisionZone> Zones { get; set; } = new List<VisionZone>();
}

public class VisionZone : Entity
{
    public int VisionSourceId { get; set; }
    public int? VisionCameraId { get; set; }
    public string ElementId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int? EquipmentId { get; set; }
    public bool Enabled { get; set; }
    public string? LastStatus { get; set; }
    public double? LastMotionPercent { get; set; }
    public DateTime? LastSeenAtUtc { get; set; }

    public VisionSource? VisionSource { get; set; }
    public VisionCamera? VisionCamera { get; set; }
    public Equipment? Equipment { get; set; }
}

public class VisionReading : Entity
{
    public int VisionSourceId { get; set; }
    public int? VisionCameraId { get; set; }
    public int? VisionZoneId { get; set; }
    public string ElementId { get; set; } = string.Empty;
    public DateTime OccurredAtUtc { get; set; }
    public DateTime CollectedAtUtc { get; set; }
    public string Metric { get; set; } = string.Empty;
    public double? NumericValue { get; set; }
    public string? TextValue { get; set; }
    public bool? BooleanValue { get; set; }
    public string? Quality { get; set; }
    public string RawJson { get; set; } = string.Empty;

    public VisionSource? VisionSource { get; set; }
    public VisionCamera? VisionCamera { get; set; }
    public VisionZone? VisionZone { get; set; }
}

public class VisionEvent : Entity
{
    public int VisionSourceId { get; set; }
    public int? VisionCameraId { get; set; }
    public int? VisionZoneId { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public DateTime CollectedAtUtc { get; set; }
    public string? Source { get; set; }
    public string Status { get; set; } = string.Empty;
    public double? MotionPercent { get; set; }
    public string? Region { get; set; }
    public string RawJson { get; set; } = string.Empty;

    public VisionSource? VisionSource { get; set; }
    public VisionCamera? VisionCamera { get; set; }
    public VisionZone? VisionZone { get; set; }
}
