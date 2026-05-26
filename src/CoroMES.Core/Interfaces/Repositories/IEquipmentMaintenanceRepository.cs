using CoroMES.Core.Entities;

namespace CoroMES.Core.Interfaces.Repositories;

/// <summary>
/// Repository for EquipmentMaintenance entity.
/// </summary>
public interface IEquipmentMaintenanceRepository : IRepository<EquipmentMaintenance>
{
    /// <summary>
    /// Get maintenance records for a specific equipment.
    /// </summary>
    Task<IEnumerable<EquipmentMaintenance>> GetByEquipmentIdAsync(int equipmentId);

    /// <summary>
    /// Get maintenance records by type.
    /// </summary>
    Task<IEnumerable<EquipmentMaintenance>> GetByTypeAsync(string maintenanceType);

    /// <summary>
    /// Get overdue maintenance tasks.
    /// </summary>
    Task<IEnumerable<EquipmentMaintenance>> GetOverdueAsync(DateTime asOf);

    /// <summary>
    /// Get maintenance history for equipment within date range.
    /// </summary>
    Task<IEnumerable<EquipmentMaintenance>> GetHistoryAsync(int equipmentId, DateTime startDate, DateTime endDate);
}
