namespace CoroMES.Integration.Upkeep;

public sealed class UpkeepOptions
{
    public string Mode { get; set; } = "mock";
    public string? BaseUrl { get; set; }
    public string? ApiKey { get; set; }
    public int TimeoutSeconds { get; set; } = 30;
}
