using CoroMES.Core.Entities;
using CoroMES.Core.Interfaces.Repositories;
using CoroMES.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CoroMES.Infrastructure.Repositories;

/// <inheritdoc/>
public class LaborRecordRepository : Repository<LaborRecord>, ILaborRecordRepository
{
    public LaborRecordRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IEnumerable<LaborRecord>> GetByOperatorAsync(int operatorId)
    {
        return await _dbSet.Where(l => l.OperatorId == operatorId).OrderByDescending(l => l.WorkDate).ToListAsync();
    }

    public async Task<IEnumerable<LaborRecord>> GetByWorkOrderAsync(int workOrderId)
    {
        return await _dbSet.Where(l => l.WorkOrderId == workOrderId).ToListAsync();
    }

    public async Task<IEnumerable<LaborRecord>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _dbSet
            .Where(l => l.WorkDate >= startDate && l.WorkDate <= endDate)
            .OrderByDescending(l => l.WorkDate)
            .ToListAsync();
    }

    public async Task<decimal> GetTotalHoursByOperatorAsync(int operatorId, DateTime date)
    {
        var start = date.Date;
        var end = start.AddDays(1);
        return await _dbSet
            .Where(l => l.OperatorId == operatorId && l.WorkDate >= start && l.WorkDate < end)
            .SumAsync(l => l.HoursWorked);
    }
}
