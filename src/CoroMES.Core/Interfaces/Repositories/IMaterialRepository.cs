using CoroMES.Core.Entities;

namespace CoroMES.Core.Interfaces.Repositories;

/// <summary>
/// Repository for Material aggregate root.
/// </summary>
public interface IMaterialRepository : IRepository<Material>
{
    /// <summary>
    /// Get material by unique code.
    /// </summary>
    Task<Material?> GetByCodeAsync(string code);

    /// <summary>
    /// Get materials with quantity below threshold (low inventory).
    /// </summary>
    Task<IEnumerable<Material>> GetLowInventoryAsync(int threshold);

    /// <summary>
    /// Get materials by type.
    /// </summary>
    Task<IEnumerable<Material>> GetByTypeAsync(string materialType);

    /// <summary>
    /// Get materials that are part of a specific bill of materials.
    /// </summary>
    Task<IEnumerable<Material>> GetByBillOfMaterialsAsync(int bomId);
}
