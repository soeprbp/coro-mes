namespace CoroMES.Integration.Upkeep;

public interface IUpkeepIntegration
{
    string Mode { get; }
    Task<IReadOnlyList<UpkeepAsset>> GetAssetsAsync(CancellationToken cancellationToken = default);
    Task<UpkeepSyncResult> SyncEquipmentAsync(UpkeepSyncRequest request, CancellationToken cancellationToken = default);
    Task<UpkeepDowntimeResult> LogDowntimeAsync(UpkeepDowntimeRequest request, CancellationToken cancellationToken = default);
}
