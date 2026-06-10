namespace CoroMES.Integration.Upkeep;

public sealed class DisabledUpkeepIntegration : IUpkeepIntegration
{
    public string Mode => "disabled";

    public Task<IReadOnlyList<UpkeepAsset>> GetAssetsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<UpkeepAsset>>([]);
    }

    public Task<UpkeepSyncResult> SyncEquipmentAsync(UpkeepSyncRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new UpkeepSyncResult(
            false,
            0,
            DateTime.UtcNow,
            Mode,
            "UpKeep integration is disabled."));
    }

    public Task<UpkeepDowntimeResult> LogDowntimeAsync(UpkeepDowntimeRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new UpkeepDowntimeResult(
            false,
            DateTime.UtcNow,
            Mode,
            "UpKeep integration is disabled."));
    }
}
