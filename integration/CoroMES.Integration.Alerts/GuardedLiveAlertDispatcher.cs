namespace CoroMES.Integration.Alerts;

public sealed class GuardedLiveAlertDispatcher(AlertingOptions options) : IAlertDispatcher
{
    public string Mode => "live";

    public IReadOnlyList<string> GetDefaultChannels(string severity)
    {
        return string.Equals(severity, "Critical", StringComparison.OrdinalIgnoreCase)
            ? Normalize(options.CriticalChannels)
            : Normalize(options.DefaultChannels);
    }

    public Task<AlertDispatchResult> DispatchAsync(AlertMessage message, CancellationToken cancellationToken = default)
    {
        var deliveries = Normalize(message.Channels)
            .Select(channel => new AlertDeliveryResult(
                channel,
                false,
                Mode,
                $"Live {channel} delivery is guarded until provider credentials and send contracts are configured."))
            .ToList();

        return Task.FromResult(new AlertDispatchResult(false, Mode, deliveries));
    }

    private static List<string> Normalize(IEnumerable<string> channels)
    {
        return channels
            .Select(channel => channel.Trim().ToLowerInvariant())
            .Where(channel => !string.IsNullOrWhiteSpace(channel))
            .Distinct()
            .ToList();
    }
}
