using CoroMES.Core.Entities;
using CoroMES.Core.Interfaces.Repositories;
using CoroMES.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CoroMES.Infrastructure.Repositories;

/// <inheritdoc/>
public class EquipmentMaintenanceRepository : Repository<EquipmentMaintenance>, IEquipmentMaintenanceRepository
{
    public EquipmentMaintenanceRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IEnumerable<EquipmentMaintenance>> GetByEquipmentIdAsync(int equipmentId)
    {
        return await _dbSet.Where(m => m.EquipmentId == equipmentId).OrderByDescending(m => m.ScheduledDate).ToListAsync();
    }

    public async Task<IEnumerable<EquipmentMaintenance>> GetByTypeAsync(string maintenanceType)
    {
        return await _dbSet.Where(m => m.MaintenanceType == maintenanceType).ToListAsync();
    }

    public async Task<IEnumerable<EquipmentMaintenance>> GetOverdueAsync(DateTime asOf)
    {
        return await _dbSet
            .Where(m => m.Status == "Scheduled" && m.ScheduledDate < asOf)
            .ToListAsync();
    }

    public async Task<IEnumerable<EquipmentMaintenance>> GetHistoryAsync(int equipmentId, DateTime startDate, DateTime endDate)
    {
        return await _dbSet
            .Where(m => m.EquipmentId == equipmentId && m.ScheduledDate >= startDate && m.ScheduledDate <= endDate)
            .OrderByDescending(m => m.ScheduledDate)
            .ToListAsync();
    }
}
