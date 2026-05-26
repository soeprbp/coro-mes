using CoroMES.Core.Entities;

namespace CoroMES.Core.Interfaces.Repositories;

/// <summary>
/// Repository for LaborRecord entity.
/// </summary>
public interface ILaborRecordRepository : IRepository<LaborRecord>
{
    /// <summary>
    /// Get labor records for an operator.
    /// </summary>
    Task<IEnumerable<LaborRecord>> GetByOperatorAsync(int operatorId);

    /// <summary>
    /// Get labor records for a work order.
    /// </summary>
    Task<IEnumerable<LaborRecord>> GetByWorkOrderAsync(int workOrderId);

    /// <summary>
    /// Get labor records by date range.
    /// </summary>
    Task<IEnumerable<LaborRecord>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);

    /// <summary>
    /// Get total labor hours for an operator on a date.
    /// </summary>
    Task<decimal> GetTotalHoursByOperatorAsync(int operatorId, DateTime date);
}
