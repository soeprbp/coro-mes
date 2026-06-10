namespace CoroMES.Integration.Alerts;

public sealed class MockAlertDispatcher(AlertingOptions options) : IAlertDispatcher
{
    public string Mode => "mock";

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
                true,
                Mode,
                $"Mock alert queued for {channel}."))
            .ToList();

        return Task.FromResult(new AlertDispatchResult(true, Mode, deliveries));
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
