using CoroMES.Core.Entities;
using CoroMES.Core.Interfaces.Repositories;
using CoroMES.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CoroMES.Infrastructure.Repositories;

/// <inheritdoc/>
public class WorkOrderOperationRepository : Repository<WorkOrderOperation>, IWorkOrderOperationRepository
{
    public WorkOrderOperationRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IEnumerable<WorkOrderOperation>> GetByWorkOrderIdAsync(int workOrderId)
    {
        return await _dbSet.Where(o => o.WorkOrderId == workOrderId).OrderBy(o => o.SequenceNumber).ToListAsync();
    }

    public async Task<IEnumerable<WorkOrderOperation>> GetByEquipmentAsync(int equipmentId)
    {
        return await _dbSet.Where(o => o.EquipmentId == equipmentId).ToListAsync();
    }

    public async Task<WorkOrderOperation?> GetByWorkOrderAndSequenceAsync(int workOrderId, int sequenceNumber)
    {
        return await _dbSet.FirstOrDefaultAsync(o => o.WorkOrderId == workOrderId && o.SequenceNumber == sequenceNumber);
    }
}
