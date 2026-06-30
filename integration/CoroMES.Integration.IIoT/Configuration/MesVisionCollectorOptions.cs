namespace CoroMES.Integration.IIoT.Configuration;

public sealed class MesVisionCollectorOptions
{
    public bool Enabled { get; set; }
    public string ExternalSystemId { get; set; } = "MES-Vision";
    public string DisplayName { get; set; } = "MES-Vision";
    public string I3XBaseUrl { get; set; } = "https://rocktumbler.57446516.xyz/i3x/v1/";
    public string? DashboardBaseUrl { get; set; } = "https://rocktumbler.57446516.xyz/";
    public string? ApiKey { get; set; }
    public int TimeoutSeconds { get; set; } = 30;
    public int PollIntervalSeconds { get; set; } = 60;
    public int HistoryLookbackMinutes { get; set; } = 60;
    public int MaxDepth { get; set; } = 1;
}
