using CoroMES.Core.Entities;

namespace CoroMES.Core.Interfaces.Repositories;

/// <summary>
/// Repository for BillOfMaterials entity.
/// </summary>
public interface IBillOfMaterialsRepository : IRepository<BillOfMaterials>
{
    /// <summary>
    /// Get BOM lines for a product (all component materials).
    /// </summary>
    Task<IEnumerable<BillOfMaterials>> GetByProductIdAsync(int productId);

    /// <summary>
    /// Get BOM lines where a specific material is used as a component.
    /// </summary>
    Task<IEnumerable<BillOfMaterials>> GetByMaterialAsync(int materialId);

    /// <summary>
    /// Get BOM line with Product and ComponentMaterial details populated.
    /// </summary>
    Task<BillOfMaterials?> GetWithItemsAsync(int bomId);
}
