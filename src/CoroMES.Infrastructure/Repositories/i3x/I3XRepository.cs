using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using CoroMES.Core.Entities;
using CoroMES.Core.Interfaces.Repositories;
using CoroMES.Industrial.i3X;
using CoroMES.Industrial.i3X.Models;
using CoroMES.Industrial.i3X.ObjectTypes;
using CoroMES.Industrial.i3X.Translators;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CoroMES.Infrastructure.Repositories.i3x;

/// <summary>
/// Generic i3X-backed repository implementation for any entity type.
/// Uses IMesEntityTranslator to convert between entity and i3X representation.
/// </summary>
/// <typeparam name="T">Entity type that inherits from Entity</typeparam>
public class I3XRepository<T> : IRepository<T> where T : Entity, new()
{
    private readonly II3XClient _i3xClient;
    private readonly IMesEntityTranslator _translator;
    private readonly ILogger<I3XRepository<T>> _logger;
    private readonly string _typeElementId;
    private readonly string _prefix;

    // Cache for entity type metadata
    private static readonly ConcurrentDictionary<Type, string> _prefixCache = new();
    private static readonly ConcurrentDictionary<Type, string> _typeElementIdCache = new();

    public I3XRepository(II3XClient i3xClient, IMesEntityTranslator translator, ILogger<I3XRepository<T>> logger)
    {
        _i3xClient = i3xClient;
        _translator = translator;
        _logger = logger;

        // Determine prefix and typeElementId from entity type
        var type = typeof(T);
        _prefix = _prefixCache.GetOrAdd(type, t =>
        {
            var dict = new Dictionary<Type, string>
            {
                [typeof(WorkOrder)] = "wo",
                [typeof(Equipment)] = "eq",
                [typeof(Material)] = "mat",
                [typeof(Operator)] = "op",
                [typeof(Inspection)] = "ins",
                [typeof(Shift)] = "shift",
                [typeof(LaborRecord)] = "labor",
                [typeof(WorkOrderOperation)] = "wo-op",
                [typeof(EquipmentMaintenance)] = "eq-maint",
                [typeof(BillOfMaterials)] = "bom",
                [typeof(MaterialMovement)] = "mat-mv",
                [typeof(InspectionItem)] = "ins-item",
                [typeof(NonConformance)] = "ncr"
            };
            return dict.TryGetValue(t, out var p) ? p : "obj";
        });

        _typeElementId = _typeElementIdCache.GetOrAdd(type, t =>
        {
            var ns = MesObjectTypes.NamespaceUri;
            return t.Name switch
            {
                nameof(WorkOrder) => $"{ns}/{MesObjectTypes.WorkOrderType}",
                nameof(Equipment) => $"{ns}/{MesObjectTypes.EquipmentType}",
                nameof(Material) => $"{ns}/{MesObjectTypes.MaterialType}",
                nameof(Operator) => $"{ns}/{MesObjectTypes.OperatorType}",
                nameof(Inspection) => $"{ns}/{MesObjectTypes.InspectionType}",
                nameof(Shift) => $"{ns}/{MesObjectTypes.ShiftType}",
                nameof(LaborRecord) => $"{ns}/{MesObjectTypes.LaborRecordType}",
                nameof(WorkOrderOperation) => $"{ns}/{MesObjectTypes.WorkOrderType}",
                nameof(EquipmentMaintenance) => $"{ns}/{MesObjectTypes.EquipmentType}",
                nameof(BillOfMaterials) => $"{ns}/{MesObjectTypes.MaterialType}",
                nameof(MaterialMovement) => $"{ns}/{MesObjectTypes.MaterialType}",
                nameof(InspectionItem) => $"{ns}/{MesObjectTypes.InspectionType}",
                nameof(NonConformance) => $"{ns}/{MesObjectTypes.InspectionType}",
                _ => throw new NotSupportedException($"Unsupported entity type: {t.Name}")
            };
        });
    }

    public async Task<T?> GetByIdAsync(int id)
    {
        var elementId = $"{_prefix}-{id}";
        var values = await _i3xClient.GetObjectValuesAsync(new List<string> { elementId }, maxDepth: 1);

        if (values.TryGetValue(elementId, out var result) && result.Value.Value != null)
        {
            var entity = ActivateEntity<T>();
            SetEntityId(entity, id);
            _translator.UpdateFromI3X((dynamic)entity, result.Value.Value);
            return entity;
        }
        return null;
    }

    public async Task<IEnumerable<T>> GetAllAsync()
    {
        // Query all objects of the matching type using typeElementId
        var objects = await _i3xClient.GetObjectsAsync(typeElementId: _typeElementId.Split('/').Last(), includeMetadata: false);

        var entities = new List<T>();
        if (objects.Any())
        {
            var elementIds = objects.Select(o => o.ElementId).ToList();
            var values = await _i3xClient.GetObjectValuesAsync(elementIds, maxDepth: 1);

            foreach (var obj in objects)
            {
                if (values.TryGetValue(obj.ElementId, out var result) && result.Value.Value != null)
                {
                    var idStr = obj.ElementId.Split('-').Last();
                    if (int.TryParse(idStr, out var id))
                    {
                        var entity = ActivateEntity<T>();
                        SetEntityId(entity, id);
                        _translator.UpdateFromI3X((dynamic)entity, result.Value.Value);
                        entities.Add(entity);
                    }
                }
            }
        }
        return entities;
    }

    public async Task AddAsync(T entity)
    {
        var obj = _translator.ToI3XObject((dynamic)entity);
        var value = _translator.ToI3XValue((dynamic)entity);
        await _i3xClient.WriteObjectValueAsync(obj.ElementId, value);
    }

    public async Task UpdateAsync(T entity)
    {
        var elementId = _translator.GenerateElementId(entity);
        var value = _translator.ToI3XValue((dynamic)entity);
        await _i3xClient.WriteObjectValueAsync(elementId, value);
    }

    public async Task DeleteAsync(int id)
    {
        _logger.LogWarning("Delete operation not directly supported in i3X for {EntityType}. Consider soft delete via status flag.", typeof(T).Name);
        await Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(int id)
    {
        var entity = await GetByIdAsync(id);
        return entity != null;
    }

    // Helper to create an instance of T (requires parameterless constructor)
    private static TEntity ActivateEntity<TEntity>() where TEntity : Entity, new()
    {
        return new TEntity();
    }

    private static void SetEntityId<TEntity>(TEntity entity, int id) where TEntity : Entity
    {
        entity.Id = id;
    }
}
