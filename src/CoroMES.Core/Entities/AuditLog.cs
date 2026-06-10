namespace CoroMES.Core.Entities;

public class AuditLog : Entity
{
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
    public string Actor { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public int? EntityId { get; set; }
    public string? Route { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public bool Succeeded { get; set; } = true;
    public string Summary { get; set; } = string.Empty;
}
