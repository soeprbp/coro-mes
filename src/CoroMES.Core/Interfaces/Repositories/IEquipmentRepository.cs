using CoroMES.Core.Entities;
using CoroMES.Core.Enums;

namespace CoroMES.Core.Interfaces.Repositories;

/// <summary>
/// Repository for Equipment aggregate root.
/// </summary>
public interface IEquipmentRepository : IRepository<Equipment>
{
    /// <summary>
    /// Get equipment by unique code.
    /// </summary>
    Task<Equipment?> GetByCodeAsync(string code);

    /// <summary>
    /// Get equipment by Upkeep asset ID.
    /// </summary>
    Task<Equipment?> GetByUpkeepAssetIdAsync(int upkeepAssetId);

    /// <summary>
    /// Get equipment by protocol type (MQTT, OPC-UA, Ethernet/IP).
    /// </summary>
    Task<IEnumerable<Equipment>> GetByProtocolAsync(string protocol);

    /// <summary>
    /// Get equipment by status (Active, Idle, Down, Maintenance).
    /// </summary>
    Task<IEnumerable<Equipment>> GetByStatusAsync(EquipmentStatus status);

    /// <summary>
    /// Get equipment with recent maintenance records.
    /// </summary>
    Task<IEnumerable<Equipment>> GetWithRecentMaintenanceAsync(DateTime since);
}
