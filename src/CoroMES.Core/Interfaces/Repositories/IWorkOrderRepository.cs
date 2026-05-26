using CoroMES.Core.Entities;
using CoroMES.Core.Enums;

namespace CoroMES.Core.Interfaces.Repositories;

/// <summary>
/// Repository for WorkOrder aggregate root.
/// </summary>
public interface IWorkOrderRepository : IRepository<WorkOrder>
{
    /// <summary>
    /// Get work orders by status.
    /// </summary>
    Task<IEnumerable<WorkOrder>> GetByStatusAsync(WorkOrderStatus status);

    /// <summary>
    /// Get work orders assigned to a specific operator.
    /// </summary>
    Task<IEnumerable<WorkOrder>> GetByOperatorAsync(int operatorId);

    /// <summary>
    /// Get active work orders (Planned or InProgress).
    /// </summary>
    Task<IEnumerable<WorkOrder>> GetActiveAsync();

    /// <summary>
    /// Get work orders by production line.
    /// </summary>
    Task<IEnumerable<WorkOrder>> GetByProductionLineAsync(string productionLineName);

    /// <summary>
    /// Get work orders by date range (scheduled start).
    /// </summary>
    Task<IEnumerable<WorkOrder>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
}
