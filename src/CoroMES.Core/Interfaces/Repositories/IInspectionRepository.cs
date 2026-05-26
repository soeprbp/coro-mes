using CoroMES.Core.Entities;

namespace CoroMES.Core.Interfaces.Repositories;

/// <summary>
/// Repository for Inspection entity.
/// </summary>
public interface IInspectionRepository : IRepository<Inspection>
{
    /// <summary>
    /// Get inspections by work order.
    /// </summary>
    Task<IEnumerable<Inspection>> GetByWorkOrderAsync(int workOrderId);

    /// <summary>
    /// Get inspections by operator.
    /// </summary>
    Task<IEnumerable<Inspection>> GetByOperatorAsync(int operatorId);

    /// <summary>
    /// Get inspections by status (Passed, Failed, Rejected).
    /// </summary>
    Task<IEnumerable<Inspection>> GetByStatusAsync(string status);

    /// <summary>
    /// Get inspections within a date range.
    /// </summary>
    Task<IEnumerable<Inspection>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);

    /// <summary>
    /// Get inspection with its items.
    /// </summary>
    Task<Inspection?> GetWithItemsAsync(int inspectionId);
}
