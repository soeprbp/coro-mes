using CoroMES.Core.Entities;
using CoroMES.Core.Interfaces.Repositories;
using CoroMES.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CoroMES.Infrastructure.Repositories;

/// <inheritdoc/>
public class BillOfMaterialsRepository : Repository<BillOfMaterials>, IBillOfMaterialsRepository
{
    public BillOfMaterialsRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IEnumerable<BillOfMaterials>> GetByProductIdAsync(int productId)
    {
        return await _dbSet.Where(b => b.ProductId == productId).ToListAsync();
    }

    public async Task<IEnumerable<BillOfMaterials>> GetByMaterialAsync(int materialId)
    {
        return await _dbSet.Where(b => b.ComponentMaterialId == materialId).ToListAsync();
    }

    public async Task<BillOfMaterials?> GetWithItemsAsync(int bomId)
    {
        return await _dbSet
            .Include(b => b.Product)
            .Include(b => b.ComponentMaterial)
            .FirstOrDefaultAsync(b => b.Id == bomId);
    }
}
