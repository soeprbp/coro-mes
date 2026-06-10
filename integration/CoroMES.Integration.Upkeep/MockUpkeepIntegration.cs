namespace CoroMES.Integration.Upkeep;

public sealed class MockUpkeepIntegration : IUpkeepIntegration
{
    private static readonly UpkeepAsset[] Assets =
    [
        new(101, "Corrugator Main Drive", "Equipment", "Building A"),
        new(102, "Bender Unit A1", "Equipment", "Building B"),
        new(103, "Conveyor Belt Line 1", "Equipment", "Building A"),
        new(104, "Temperature Sensor Array", "Sensor", "Building A")
    ];

    public string Mode => "mock";

    public Task<IReadOnlyList<UpkeepAsset>> GetAssetsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<UpkeepAsset>>(Assets);
    }

    public Task<UpkeepSyncResult> SyncEquipmentAsync(UpkeepSyncRequest request, CancellationToken cancellationToken = default)
    {
        var result = new UpkeepSyncResult(
            true,
            request.Equipment.Count,
            DateTime.UtcNow,
            Mode,
            $"Mock sync accepted {request.Equipment.Count} linked equipment records.");

        return Task.FromResult(result);
    }

    public Task<UpkeepDowntimeResult> LogDowntimeAsync(UpkeepDowntimeRequest request, CancellationToken cancellationToken = default)
    {
        var result = new UpkeepDowntimeResult(
            true,
            DateTime.UtcNow,
            Mode,
            "Mock downtime event accepted.");

        return Task.FromResult(result);
    }
}
