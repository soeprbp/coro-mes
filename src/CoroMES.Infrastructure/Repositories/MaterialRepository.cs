using CoroMES.Core.Entities;
using CoroMES.Core.Interfaces.Repositories;
using CoroMES.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CoroMES.Infrastructure.Repositories;

/// <inheritdoc/>
public class MaterialRepository : Repository<Material>, IMaterialRepository
{
    public MaterialRepository(ApplicationDbContext context) : base(context) { }

    public async Task<Material?> GetByCodeAsync(string code)
    {
        return await _dbSet.FirstOrDefaultAsync(m => m.Code == code);
    }

    public async Task<IEnumerable<Material>> GetLowInventoryAsync(int threshold)
    {
        return await _dbSet.Where(m => m.CurrentQuantity < threshold).ToListAsync();
    }

    public async Task<IEnumerable<Material>> GetByTypeAsync(string materialType)
    {
        return await _dbSet.Where(m => m.Category == materialType).ToListAsync();
    }

    public async Task<IEnumerable<Material>> GetByBillOfMaterialsAsync(int productId)
    {
        // Get component materials for a product (finished good)
        var materialIds = await _context.BillOfMaterials
            .Where(b => b.ProductId == productId)
            .Select(b => b.ComponentMaterialId)
            .ToListAsync();

        return await _dbSet.Where(m => materialIds.Contains(m.Id)).ToListAsync();
    }
}
