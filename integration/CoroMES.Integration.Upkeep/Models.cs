namespace CoroMES.Integration.Upkeep;

public sealed record UpkeepAsset(int Id, string Name, string Type, string Location, string Source = "mock");

public sealed record UpkeepEquipmentLink(
    int EquipmentId,
    string Code,
    string Name,
    int UpkeepAssetId,
    string Status,
    string? Location);

public sealed record UpkeepSyncRequest(IReadOnlyList<UpkeepEquipmentLink> Equipment);

public sealed record UpkeepSyncResult(
    bool Success,
    int Synced,
    DateTime TimestampUtc,
    string Mode,
    string Message);

public sealed record UpkeepDowntimeRequest(
    int? EquipmentId,
    int? UpkeepAssetId,
    string? Reason,
    DateTime OccurredAtUtc);

public sealed record UpkeepDowntimeResult(
    bool Success,
    DateTime TimestampUtc,
    string Mode,
    string Message);
