using CoroMES.Core.Entities;

namespace CoroMES.Core.Interfaces.Repositories;

/// <summary>
/// Repository for MaterialMovement entity.
/// </summary>
public interface IMaterialMovementRepository : IRepository<MaterialMovement>
{
    /// <summary>
    /// Get movements for a specific material.
    /// </summary>
    Task<IEnumerable<MaterialMovement>> GetByMaterialAsync(int materialId);

    /// <summary>
    /// Get movements by type (Receipt, Issue, Adjustment, etc.).
    /// </summary>
    Task<IEnumerable<MaterialMovement>> GetByTypeAsync(string movementType);

    /// <summary>
    /// Get movements within a date range.
    /// </summary>
    Task<IEnumerable<MaterialMovement>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);

    /// <summary>
    /// Get recent movements (last N days).
    /// </summary>
    Task<IEnumerable<MaterialMovement>> GetRecentAsync(int days);
}
