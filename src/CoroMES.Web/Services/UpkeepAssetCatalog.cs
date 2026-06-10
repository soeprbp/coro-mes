namespace CoroMES.Web.Services;

public interface IUpkeepAssetCatalog
{
    IReadOnlyList<UpkeepAsset> GetAssets();
}

public sealed class UpkeepAssetCatalog : IUpkeepAssetCatalog
{
    private static readonly UpkeepAsset[] Assets =
    [
        new(101, "Corrugator Main Drive", "Equipment", "Building A"),
        new(102, "Bender Unit A1", "Equipment", "Building B"),
        new(103, "Conveyor Belt Line 1", "Equipment", "Building A"),
        new(104, "Temperature Sensor Array", "Sensor", "Building A")
    ];

    public IReadOnlyList<UpkeepAsset> GetAssets() => Assets;
}

public sealed record UpkeepAsset(int Id, string Name, string Type, string Location);
