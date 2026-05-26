using CoroMES.Core.Entities;
using CoroMES.Core.Interfaces.Repositories;
using CoroMES.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CoroMES.Infrastructure.Repositories;

/// <inheritdoc/>
public class InspectionItemRepository : Repository<InspectionItem>, IInspectionItemRepository
{
    public InspectionItemRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IEnumerable<InspectionItem>> GetByInspectionAsync(int inspectionId)
    {
        return await _dbSet.Where(ii => ii.InspectionId == inspectionId).ToListAsync();
    }

    public async Task<IEnumerable<InspectionItem>> GetFailedAsync(int? inspectionId = null)
    {
        var query = _dbSet.Where(ii => ii.Status == "Fail");
        if (inspectionId.HasValue)
        {
            query = query.Where(ii => ii.InspectionId == inspectionId.Value);
        }
        return await query.ToListAsync();
    }
}
