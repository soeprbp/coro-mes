using CoroMES.Core.Entities;
using CoroMES.Core.Interfaces.Repositories;
using CoroMES.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CoroMES.Infrastructure.Repositories;

/// <inheritdoc/>
public class InspectionRepository : Repository<Inspection>, IInspectionRepository
{
    public InspectionRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IEnumerable<Inspection>> GetByWorkOrderAsync(int workOrderId)
    {
        return await _dbSet.Where(i => i.WorkOrderId == workOrderId).OrderByDescending(i => i.InspectionDate).ToListAsync();
    }

    public async Task<IEnumerable<Inspection>> GetByOperatorAsync(int operatorId)
    {
        return await _dbSet.Where(i => i.OperatorId == operatorId).ToListAsync();
    }

    public async Task<IEnumerable<Inspection>> GetByStatusAsync(string status)
    {
        return await _dbSet.Where(i => i.Status == status).ToListAsync();
    }

    public async Task<IEnumerable<Inspection>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _dbSet
            .Where(i => i.InspectionDate >= startDate && i.InspectionDate <= endDate)
            .OrderByDescending(i => i.InspectionDate)
            .ToListAsync();
    }

    public async Task<Inspection?> GetWithItemsAsync(int inspectionId)
    {
        return await _dbSet
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.Id == inspectionId);
    }
}
