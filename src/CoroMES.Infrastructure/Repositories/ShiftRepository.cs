using CoroMES.Core.Entities;
using CoroMES.Core.Interfaces.Repositories;
using CoroMES.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CoroMES.Infrastructure.Repositories;

/// <inheritdoc/>
public class ShiftRepository : Repository<Shift>, IShiftRepository
{
    public ShiftRepository(ApplicationDbContext context) : base(context) { }

    public async Task<Shift?> GetByCodeAsync(string code)
    {
        return await _dbSet.FirstOrDefaultAsync(s => s.Code == code);
    }

    public async Task<IEnumerable<Shift>> GetActiveAsync(DateTime asOf)
    {
        // Determine active shifts based on start/end times ( simplistic )
        // This would need actual time logic; for now return all
        return await _dbSet.ToListAsync();
    }

    public async Task<Shift?> GetWithOperatorsAsync(int shiftId)
    {
        return await _dbSet
            .Include(s => s.Operators)
            .FirstOrDefaultAsync(s => s.Id == shiftId);
    }
}
