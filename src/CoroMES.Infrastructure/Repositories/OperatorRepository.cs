using CoroMES.Core.Entities;
using CoroMES.Core.Interfaces.Repositories;
using CoroMES.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CoroMES.Infrastructure.Repositories;

/// <inheritdoc/>
public class OperatorRepository : Repository<Operator>, IOperatorRepository
{
    public OperatorRepository(ApplicationDbContext context) : base(context) { }

    public async Task<Operator?> GetByEmployeeNumberAsync(string employeeNumber)
    {
        return await _dbSet.FirstOrDefaultAsync(o => o.EmployeeNumber == employeeNumber);
    }

    public async Task<IEnumerable<Operator>> GetByShiftAsync(int shiftId)
    {
        return await _dbSet.Where(o => o.ShiftId == shiftId).ToListAsync();
    }

    public async Task<IEnumerable<Operator>> GetOnDutyAsync(DateTime asOf)
    {
        // Simplified: check if operator has a shift that covers the time
        // In real implementation, would cross-reference Shift schedule
        return await _dbSet
            .Include(o => o.Shift)
            .Where(o => o.Shift != null)
            .ToListAsync();
    }

    public async Task<Operator?> GetWithLaborRecordsAsync(int operatorId)
    {
        return await _dbSet
            .Include(o => o.LaborRecords)
            .FirstOrDefaultAsync(o => o.Id == operatorId);
    }
}
