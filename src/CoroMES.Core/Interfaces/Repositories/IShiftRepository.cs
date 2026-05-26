using CoroMES.Core.Entities;

namespace CoroMES.Core.Interfaces.Repositories;

/// <summary>
/// Repository for Shift entity.
/// </summary>
public interface IShiftRepository : IRepository<Shift>
{
    /// <summary>
    /// Get shift by code (e.g., "A", "B", "C").
    /// </summary>
    Task<Shift?> GetByCodeAsync(string code);

    /// <summary>
    /// Get active shifts at a specific time.
    /// </summary>
    Task<IEnumerable<Shift>> GetActiveAsync(DateTime asOf);

    /// <summary>
    /// Get shift with its operators.
    /// </summary>
    Task<Shift?> GetWithOperatorsAsync(int shiftId);
}
