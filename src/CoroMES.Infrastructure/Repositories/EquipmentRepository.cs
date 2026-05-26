using CoroMES.Core.Entities;
using CoroMES.Core.Interfaces.Repositories;
using CoroMES.Core.Enums;
using CoroMES.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CoroMES.Infrastructure.Repositories;

/// <inheritdoc/>
public class EquipmentRepository : Repository<Equipment>, IEquipmentRepository
{
    public EquipmentRepository(ApplicationDbContext context) : base(context) { }

    public async Task<Equipment?> GetByCodeAsync(string code)
    {
        return await _dbSet.FirstOrDefaultAsync(e => e.Code == code);
    }

    public async Task<Equipment?> GetByUpkeepAssetIdAsync(int upkeepAssetId)
    {
        return await _dbSet.FirstOrDefaultAsync(e => e.UpkeepAssetId == upkeepAssetId);
    }

    public async Task<IEnumerable<Equipment>> GetByProtocolAsync(string protocol)
    {
        return await _dbSet.Where(e => e.Protocol == protocol).ToListAsync();
    }

    public async Task<IEnumerable<Equipment>> GetByStatusAsync(EquipmentStatus status)
    {
        return await _dbSet.Where(e => e.Status == status).ToListAsync();
    }

    public async Task<IEnumerable<Equipment>> GetWithRecentMaintenanceAsync(DateTime since)
    {
        return await _dbSet
            .Include(e => e.Maintenances)
            .Where(e => e.Maintenances.Any(m => m.ScheduledDate >= since))
            .ToListAsync();
    }
}
