namespace CoroMES.Integration.Alerts;

public sealed class AlertingOptions
{
    public string Mode { get; set; } = "mock";
    public List<string> DefaultChannels { get; set; } = ["email"];
    public List<string> CriticalChannels { get; set; } = ["email", "sms", "pushover", "upkeep"];
}
