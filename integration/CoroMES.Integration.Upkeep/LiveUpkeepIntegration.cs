using System.Net.Http.Json;

namespace CoroMES.Integration.Upkeep;

public sealed class LiveUpkeepIntegration(HttpClient httpClient, UpkeepOptions options) : IUpkeepIntegration
{
    public string Mode => "live";

    public async Task<IReadOnlyList<UpkeepAsset>> GetAssetsAsync(CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        using var request = new HttpRequestMessage(HttpMethod.Get, "assets");
        ApplyAuthorization(request);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var assets = await response.Content.ReadFromJsonAsync<List<UpkeepAsset>>(cancellationToken: cancellationToken);
        return assets ?? [];
    }

    public Task<UpkeepSyncResult> SyncEquipmentAsync(UpkeepSyncRequest request, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        return Task.FromResult(new UpkeepSyncResult(
            false,
            0,
            DateTime.UtcNow,
            Mode,
            "Live UpKeep sync is not enabled until the final UpKeep write API contract is confirmed."));
    }

    public Task<UpkeepDowntimeResult> LogDowntimeAsync(UpkeepDowntimeRequest request, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        return Task.FromResult(new UpkeepDowntimeResult(
            false,
            DateTime.UtcNow,
            Mode,
            "Live UpKeep downtime writes are not enabled until the final UpKeep write API contract is confirmed."));
    }

    private void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(options.BaseUrl) || string.IsNullOrWhiteSpace(options.ApiKey))
        {
            throw new InvalidOperationException("Live UpKeep integration requires Upkeep:BaseUrl and Upkeep:ApiKey configuration.");
        }
    }

    private void ApplyAuthorization(HttpRequestMessage request)
    {
        request.Headers.Authorization = new("Bearer", options.ApiKey);
    }
}
