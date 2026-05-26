using CoroMES.Core.Entities;

namespace CoroMES.Core.Interfaces.Repositories;

/// <summary>
/// Repository for WorkOrderOperation entity.
/// </summary>
public interface IWorkOrderOperationRepository : IRepository<WorkOrderOperation>
{
    /// <summary>
    /// Get operations for a specific work order.
    /// </summary>
    Task<IEnumerable<WorkOrderOperation>> GetByWorkOrderIdAsync(int workOrderId);

    /// <summary>
    /// Get operations by equipment.
    /// </summary>
    Task<IEnumerable<WorkOrderOperation>> GetByEquipmentAsync(int equipmentId);

    /// <summary>
    /// Get operation by sequence number within a work order.
    /// </summary>
    Task<WorkOrderOperation?> GetByWorkOrderAndSequenceAsync(int workOrderId, int sequenceNumber);
}
