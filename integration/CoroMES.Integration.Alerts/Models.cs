namespace CoroMES.Integration.Alerts;

public sealed record AlertMessage(
    string Title,
    string Body,
    string Severity,
    string Source,
    int? EquipmentId,
    IReadOnlyList<string> Channels);

public sealed record AlertDeliveryResult(
    string Channel,
    bool Success,
    string Mode,
    string Message);

public sealed record AlertDispatchResult(
    bool Success,
    string Mode,
    IReadOnlyList<AlertDeliveryResult> Deliveries)
{
    public string Summary => Deliveries.Count == 0
        ? $"No alert channels configured in {Mode} mode."
        : string.Join("; ", Deliveries.Select(item => $"{item.Channel}:{(item.Success ? "ok" : "blocked")}"));
}
