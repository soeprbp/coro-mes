using CoroMES.Core.Entities;

namespace CoroMES.Core.Interfaces.Repositories;

/// <summary>
/// Repository for NonConformance entity.
/// </summary>
public interface INonConformanceRepository : IRepository<NonConformance>
{
    /// <summary>
    /// Get non-conformances by work order.
    /// </summary>
    Task<IEnumerable<NonConformance>> GetByWorkOrderAsync(int workOrderId);

    /// <summary>
    /// Get non-conformances by inspector.
    /// </summary>
    Task<IEnumerable<NonConformance>> GetByInspectorAsync(int inspectorId);

    /// <summary>
    /// Get open (unresolved) non-conformances.
    /// </summary>
    Task<IEnumerable<NonConformance>> GetOpenAsync();

    /// <summary>
    /// Get non-conformances by severity.
    /// </summary>
    Task<IEnumerable<NonConformance>> GetBySeverityAsync(string severity);

    /// <summary>
    /// Get non-conformance with related inspection.
    /// </summary>
    Task<NonConformance?> GetWithInspectionAsync(int ncrId);
}
