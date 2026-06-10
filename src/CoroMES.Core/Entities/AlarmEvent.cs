using CoroMES.Core.Enums;

namespace CoroMES.Core.Entities;

public class AlarmEvent : Entity
{
    public string Source { get; set; } = "manual";
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public AlarmSeverity Severity { get; set; } = AlarmSeverity.Warning;
    public AlarmStatus Status { get; set; } = AlarmStatus.Active;
    public int? EquipmentId { get; set; }
    public DateTime TriggeredAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? AcknowledgedAtUtc { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }
    public string? AcknowledgedBy { get; set; }
    public string? ResolvedBy { get; set; }
    public string? AlertChannels { get; set; }
    public string? NotificationSummary { get; set; }

    public Equipment? Equipment { get; set; }
}
