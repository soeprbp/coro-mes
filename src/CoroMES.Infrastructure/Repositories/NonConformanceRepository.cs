using CoroMES.Core.Entities;
using CoroMES.Core.Interfaces.Repositories;
using CoroMES.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CoroMES.Infrastructure.Repositories;

/// <inheritdoc/>
public class NonConformanceRepository : Repository<NonConformance>, INonConformanceRepository
{
    public NonConformanceRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IEnumerable<NonConformance>> GetByWorkOrderAsync(int workOrderId)
    {
        return await _dbSet.Where(n => n.WorkOrderId == workOrderId).OrderByDescending(n => n.CreatedAt).ToListAsync();
    }

    public async Task<IEnumerable<NonConformance>> GetByInspectorAsync(int inspectorId)
    {
        return await _dbSet.Where(n => n.OperatorId == inspectorId).ToListAsync();
    }

    public async Task<IEnumerable<NonConformance>> GetOpenAsync()
    {
        return await _dbSet.Where(n => n.Status != "Closed" && n.Status != "Resolved").ToListAsync();
    }

    public async Task<IEnumerable<NonConformance>> GetBySeverityAsync(string severity)
    {
        return await _dbSet.Where(n => n.Severity == severity).ToListAsync();
    }

    public async Task<NonConformance?> GetWithInspectionAsync(int ncrId)
    {
        return await _dbSet
            .Include(n => n.Inspection)
            .FirstOrDefaultAsync(n => n.Id == ncrId);
    }
}
