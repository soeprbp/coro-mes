using CoroMES.Core.Entities;

namespace CoroMES.Core.Interfaces.Repositories;

/// <summary>
/// Repository for InspectionItem entity.
/// </summary>
public interface IInspectionItemRepository : IRepository<InspectionItem>
{
    /// <summary>
    /// Get items for a specific inspection.
    /// </summary>
    Task<IEnumerable<InspectionItem>> GetByInspectionAsync(int inspectionId);

    /// <summary>
    /// Get failed inspection items.
    /// </summary>
    Task<IEnumerable<InspectionItem>> GetFailedAsync(int? inspectionId = null);
}
