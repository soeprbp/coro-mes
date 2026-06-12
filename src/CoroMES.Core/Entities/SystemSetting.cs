namespace CoroMES.Core.Entities;

public class SystemSetting : Entity
{
    public string? UserId { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
