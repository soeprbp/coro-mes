namespace CoroMES.Integration.Alerts;

public interface IAlertDispatcher
{
    string Mode { get; }
    IReadOnlyList<string> GetDefaultChannels(string severity);
    Task<AlertDispatchResult> DispatchAsync(AlertMessage message, CancellationToken cancellationToken = default);
}
