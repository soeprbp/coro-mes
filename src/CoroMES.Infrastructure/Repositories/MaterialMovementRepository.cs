using CoroMES.Core.Entities;
using CoroMES.Core.Interfaces.Repositories;
using CoroMES.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CoroMES.Infrastructure.Repositories;

/// <inheritdoc/>
public class MaterialMovementRepository : Repository<MaterialMovement>, IMaterialMovementRepository
{
    public MaterialMovementRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IEnumerable<MaterialMovement>> GetByMaterialAsync(int materialId)
    {
        return await _dbSet.Where(m => m.MaterialId == materialId).OrderByDescending(m => m.MovementDate).ToListAsync();
    }

    public async Task<IEnumerable<MaterialMovement>> GetByTypeAsync(string movementType)
    {
        return await _dbSet.Where(m => m.MovementType == movementType).ToListAsync();
    }

    public async Task<IEnumerable<MaterialMovement>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _dbSet
            .Where(m => m.MovementDate >= startDate && m.MovementDate <= endDate)
            .OrderByDescending(m => m.MovementDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<MaterialMovement>> GetRecentAsync(int days)
    {
        var since = DateTime.UtcNow.AddDays(-days);
        return await _dbSet.Where(m => m.MovementDate >= since).OrderByDescending(m => m.MovementDate).ToListAsync();
    }
}
