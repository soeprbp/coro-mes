namespace CoroMES.Integration.Alerts;

public sealed class DisabledAlertDispatcher : IAlertDispatcher
{
    public string Mode => "disabled";

    public IReadOnlyList<string> GetDefaultChannels(string severity)
    {
        return [];
    }

    public Task<AlertDispatchResult> DispatchAsync(AlertMessage message, CancellationToken cancellationToken = default)
    {
        var deliveries = message.Channels
            .Select(channel => new AlertDeliveryResult(
                channel,
                false,
                Mode,
                "Alerting is disabled."))
            .ToList();

        return Task.FromResult(new AlertDispatchResult(false, Mode, deliveries));
    }
}
