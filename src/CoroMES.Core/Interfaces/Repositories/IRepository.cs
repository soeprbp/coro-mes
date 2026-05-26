namespace CoroMES.Core.Interfaces.Repositories;

/// <summary>
/// Generic repository interface for CRUD operations on entities.
/// </summary>
/// <typeparam name="T">Entity type that inherits from <see cref="Core.Entities.Entity"/></typeparam>
public interface IRepository<T> where T : Core.Entities.Entity
{
    /// <summary>
    /// Get an entity by its ID.
    /// </summary>
    Task<T?> GetByIdAsync(int id);

    /// <summary>
    /// Get all entities.
    /// </summary>
    Task<IEnumerable<T>> GetAllAsync();

    /// <summary>
    /// Add a new entity.
    /// </summary>
    Task AddAsync(T entity);

    /// <summary>
    /// Update an existing entity.
    /// </summary>
    Task UpdateAsync(T entity);

    /// <summary>
    /// Delete an entity by ID.
    /// </summary>
    Task DeleteAsync(int id);

    /// <summary>
    /// Check if an entity exists.
    /// </summary>
    Task<bool> ExistsAsync(int id);
}
