namespace CoroMES.Core.Entities;

public class DisplayDefinition : Entity
{
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "oee";
    public int RefreshSeconds { get; set; } = 5;
    public int? EquipmentId { get; set; }
    public bool IsActive { get; set; } = true;
    public string? SettingsJson { get; set; }

    public Equipment? Equipment { get; set; }
}
