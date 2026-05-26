using CoroMES.Core.Entities;
using CoroMES.Core.Enums;
using CoroMES.Core.Interfaces.Repositories;
using CoroMES.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CoroMES.Infrastructure.Repositories;

/// <inheritdoc/>
public class WorkOrderRepository : Repository<WorkOrder>, IWorkOrderRepository
{
    public WorkOrderRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IEnumerable<WorkOrder>> GetByStatusAsync(WorkOrderStatus status)
    {
        return await _dbSet.Where(w => w.Status == status).ToListAsync();
    }

    public async Task<IEnumerable<WorkOrder>> GetByOperatorAsync(int operatorId)
    {
        return await _dbSet.Where(w => w.OperatorId == operatorId).ToListAsync();
    }

    public async Task<IEnumerable<WorkOrder>> GetActiveAsync()
    {
        return await _dbSet.Where(w => w.Status == WorkOrderStatus.Planned || w.Status == WorkOrderStatus.InProgress).ToListAsync();
    }

    public async Task<IEnumerable<WorkOrder>> GetByProductionLineAsync(string productionLineName)
    {
        return await _dbSet.Where(w => w.ProductionLineName == productionLineName).ToListAsync();
    }

    public async Task<IEnumerable<WorkOrder>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _dbSet.Where(w => w.ScheduledStartDate >= startDate && w.ScheduledStartDate <= endDate).ToListAsync();
    }
}
