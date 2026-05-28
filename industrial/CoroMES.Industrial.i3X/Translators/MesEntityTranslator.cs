using CoroMES.Core.Entities;
using CoroMES.Industrial.i3X.Models;
using CoroMES.Industrial.i3X.ObjectTypes;
using System.Text.Json;

namespace CoroMES.Industrial.i3X.Translators;

/// <summary>
/// Translates between CoroMES domain entities and i3X objects.
/// </summary>
public interface IMesEntityTranslator
{
    // Entity → i3X Object (metadata only)
    ObjectInstance ToI3XObject(WorkOrder workOrder);
    ObjectInstance ToI3XObject(Equipment equipment);
    ObjectInstance ToI3XObject(Material material);
    ObjectInstance ToI3XObject(Operator op);
    ObjectInstance ToI3XObject(Inspection inspection);
    ObjectInstance ToI3XObject(Shift shift);
    ObjectInstance ToI3XObject(LaborRecord laborRecord);
    ObjectInstance ToI3XObject(WorkOrderOperation operation);
    ObjectInstance ToI3XObject(EquipmentMaintenance maintenance);
    ObjectInstance ToI3XObject(BillOfMaterials bom);
    ObjectInstance ToI3XObject(MaterialMovement movement);
    ObjectInstance ToI3XObject(InspectionItem item);
    ObjectInstance ToI3XObject(NonConformance ncr);

    // Entity → i3X Value (full state as JSON-serializable object)
    object ToI3XValue(WorkOrder workOrder);
    object ToI3XValue(Equipment equipment);
    object ToI3XValue(Material material);
    object ToI3XValue(Operator op);
    object ToI3XValue(Inspection inspection);
    object ToI3XValue(Shift shift);
    object ToI3XValue(LaborRecord laborRecord);
    object ToI3XValue(WorkOrderOperation operation);
    object ToI3XValue(EquipmentMaintenance maintenance);
    object ToI3XValue(BillOfMaterials bom);
    object ToI3XValue(MaterialMovement movement);
    object ToI3XValue(InspectionItem item);
    object ToI3XValue(NonConformance ncr);

    // i3X Value → Entity (full update from i3X state)
    void UpdateFromI3X(WorkOrder workOrder, object value);
    void UpdateFromI3X(Equipment equipment, object value);
    void UpdateFromI3X(Material material, object value);
    void UpdateFromI3X(Operator op, object value);
    void UpdateFromI3X(Inspection inspection, object value);
    void UpdateFromI3X(Shift shift, object value);
    void UpdateFromI3X(LaborRecord laborRecord, object value);
    void UpdateFromI3X(WorkOrderOperation operation, object value);
    void UpdateFromI3X(EquipmentMaintenance maintenance, object value);
    void UpdateFromI3X(BillOfMaterials bom, object value);
    void UpdateFromI3X(MaterialMovement movement, object value);
    void UpdateFromI3X(InspectionItem item, object value);
    void UpdateFromI3X(NonConformance ncr, object value);

    // Generate i3X elementId for an entity
    string GenerateElementId<T>(T entity) where T : Entity;
}

/// <summary>
/// Default implementation of entity translator using JSON serialization.
/// </summary>
public class MesEntityTranslator : IMesEntityTranslator
{
    private readonly Dictionary<Type, string> _elementIdPrefixes = new()
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

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    public ObjectInstance ToI3XObject(WorkOrder workOrder) => CreateBaseObject(workOrder, MesObjectTypes.WorkOrderType, workOrder.ProductionLineId?.ToString());
    public ObjectInstance ToI3XObject(Equipment equipment) => CreateBaseObject(equipment, MesObjectTypes.EquipmentType, equipment.ProductionLineId?.ToString());
    public ObjectInstance ToI3XObject(Material material) => CreateBaseObject(material, MesObjectTypes.MaterialType, null);
    public ObjectInstance ToI3XObject(Operator op) => CreateBaseObject(op, MesObjectTypes.OperatorType, op.ShiftId?.ToString());
    public ObjectInstance ToI3XObject(Inspection inspection) => CreateBaseObject(inspection, MesObjectTypes.InspectionType, inspection.WorkOrderId?.ToString());
    public ObjectInstance ToI3XObject(Shift shift) => CreateBaseObject(shift, MesObjectTypes.ShiftType, null);
    public ObjectInstance ToI3XObject(LaborRecord laborRecord) => CreateBaseObject(laborRecord, MesObjectTypes.LaborRecordType, laborRecord.OperatorId.ToString());

    private ObjectInstance CreateBaseObject<T>(T entity, string typeId, string? parentId) where T : Entity
    {
        return new ObjectInstance
        {
            ElementId = GenerateElementId(entity),
            DisplayName = GetDisplayName(entity),
            TypeId = typeId,
            ParentId = parentId,
            IsComposition = false,
            IsExtended = false
        };
    }

    private string GetDisplayName<T>(T entity) where T : Entity
    {
        return entity switch
        {
            WorkOrder wo => wo.Number,
            Equipment eq => eq.Name,
            Material mat => mat.Name,
            Operator op => $"{op.FirstName} {op.LastName}",
            Inspection ins => $"Inspection #{ins.Id}",
            Shift shift => shift.Name,
            LaborRecord lr => $"Labor {lr.Id}",
            _ => $"{typeof(T).Name} {entity.Id}"
        };
    }

    public object ToI3XValue(WorkOrder workOrder)
    {
        return new
        {
            id = workOrder.Id,
            number = workOrder.Number,
            description = workOrder.Description,
            status = workOrder.Status.ToString(),
            priority = workOrder.Priority.ToString(),
            quantityPlanned = workOrder.QuantityPlanned,
            quantityCompleted = workOrder.QuantityCompleted,
            quantityScrap = workOrder.QuantityScrap,
            productId = workOrder.ProductId,
            productName = workOrder.ProductName,
            productionLineId = workOrder.ProductionLineId,
            productionLineName = workOrder.ProductionLineName,
            operatorId = workOrder.OperatorId,
            operatorName = workOrder.OperatorName,
            scheduledStartDate = workOrder.ScheduledStartDate?.ToString("o"),
            scheduledEndDate = workOrder.ScheduledEndDate?.ToString("o"),
            actualStartDate = workOrder.ActualStartDate?.ToString("o"),
            actualEndDate = workOrder.ActualEndDate?.ToString("o"),
            customerOrderNumber = workOrder.CustomerOrderNumber,
            notes = workOrder.Notes,
            createdAt = workOrder.CreatedAt.ToString("o"),
            updatedAt = workOrder.UpdatedAt?.ToString("o")
        };
    }

    public object ToI3XValue(Equipment equipment)
    {
        return new
        {
            id = equipment.Id,
            code = equipment.Code,
            name = equipment.Name,
            description = equipment.Description,
            type = equipment.Type.ToString(),
            status = equipment.Status.ToString(),
            serialNumber = equipment.SerialNumber,
            model = equipment.Model,
            manufacturer = equipment.Manufacturer,
            location = equipment.Location,
            ipAddress = equipment.IpAddress,
            isActive = equipment.IsActive,
            lastMaintenanceDate = equipment.LastMaintenanceDate?.ToString("o"),
            upkeepAssetId = equipment.UpkeepAssetId,
            partsPerMinute = equipment.PartsPerMinute,
            sqFtPerDay = equipment.SqFtPerDay,
            cycleTimeSeconds = equipment.CycleTimeSeconds,
            protocol = equipment.Protocol,
            createdAt = equipment.CreatedAt.ToString("o"),
            updatedAt = equipment.UpdatedAt?.ToString("o")
        };
    }

    public object ToI3XValue(Material material)
    {
        return new
        {
            id = material.Id,
            code = material.Code,
            name = material.Name,
            description = material.Description,
            unitOfMeasure = material.UnitOfMeasure,
            unitCost = material.UnitCost,
            category = material.Category,
            currentQuantity = material.CurrentQuantity,
            minimumQuantity = material.MinimumQuantity,
            maximumQuantity = material.MaximumQuantity,
            location = material.Location,
            isActive = material.IsActive,
            supplierId = material.SupplierId,
            supplierName = material.SupplierName,
            createdAt = material.CreatedAt.ToString("o"),
            updatedAt = material.UpdatedAt?.ToString("o")
        };
    }

    public object ToI3XValue(Operator op)
    {
        return new
        {
            id = op.Id,
            employeeNumber = op.EmployeeNumber,
            firstName = op.FirstName,
            lastName = op.LastName,
            email = op.Email,
            phone = op.Phone,
            isActive = op.IsActive,
            department = op.Department,
            role = op.Role,
            shiftId = op.ShiftId,
            upkeepUserId = op.UpkeepUserId,
            createdAt = op.CreatedAt.ToString("o"),
            updatedAt = op.UpdatedAt?.ToString("o")
        };
    }

    public object ToI3XValue(Inspection inspection)
    {
        return new
        {
            id = inspection.Id,
            workOrderId = inspection.WorkOrderId,
            equipmentId = inspection.EquipmentId,
            operatorId = inspection.OperatorId,
            inspectionType = inspection.InspectionType,
            inspectionDate = inspection.InspectionDate.ToString("o"),
            status = inspection.Status,
            quantityInspected = inspection.QuantityInspected,
            quantityPassed = inspection.QuantityPassed,
            quantityFailed = inspection.QuantityFailed,
            result = inspection.Result,
            notes = inspection.Notes,
            createdAt = inspection.CreatedAt.ToString("o"),
            updatedAt = inspection.UpdatedAt?.ToString("o")
        };
    }

    public object ToI3XValue(Shift shift)
    {
        return new
        {
            id = shift.Id,
            code = shift.Code,
            name = shift.Name,
            startTime = shift.StartTime.ToString(@"hh\:mm"),
            endTime = shift.EndTime.ToString(@"hh\:mm"),
            productionLineId = shift.ProductionLineId,
            productionLineName = shift.ProductionLineName,
            isActive = shift.IsActive,
            createdAt = shift.CreatedAt.ToString("o"),
            updatedAt = shift.UpdatedAt?.ToString("o")
        };
    }

    public object ToI3XValue(LaborRecord laborRecord)
    {
        return new
        {
            id = laborRecord.Id,
            operatorId = laborRecord.OperatorId,
            shiftId = laborRecord.ShiftId,
            workOrderId = laborRecord.WorkOrderId,
            equipmentId = laborRecord.EquipmentId,
            workDate = laborRecord.WorkDate.ToString("o"),
            hoursWorked = laborRecord.HoursWorked,
            activityCode = laborRecord.ActivityCode,
            notes = laborRecord.Notes,
            createdAt = laborRecord.CreatedAt.ToString("o"),
            updatedAt = laborRecord.UpdatedAt?.ToString("o")
        };
    }

    public ObjectInstance ToI3XObject(WorkOrderOperation operation)
    {
        return new ObjectInstance
        {
            ElementId = GenerateElementId(operation),
            DisplayName = $"Op {operation.SequenceNumber}: {operation.OperationName}",
            TypeId = MesObjectTypes.WorkOrderType, // Would need separate type; reuse for now
            ParentId = operation.WorkOrderId.ToString(),
            IsComposition = true,
            IsExtended = false
        };
    }

    public ObjectInstance ToI3XObject(EquipmentMaintenance maintenance)
    {
        return new ObjectInstance
        {
            ElementId = GenerateElementId(maintenance),
            DisplayName = $"{maintenance.MaintenanceType} on {maintenance.EquipmentId}",
            TypeId = MesObjectTypes.EquipmentType, // Reuse
            ParentId = maintenance.EquipmentId.ToString(),
            IsComposition = false,
            IsExtended = false
        };
    }

    public ObjectInstance ToI3XObject(BillOfMaterials bom)
    {
        return new ObjectInstance
        {
            ElementId = GenerateElementId(bom),
            DisplayName = $"BOM for Product {bom.ProductId}",
            TypeId = MesObjectTypes.MaterialType, // Reuse
            IsComposition = false,
            IsExtended = false
        };
    }

    public ObjectInstance ToI3XObject(MaterialMovement movement)
    {
        return new ObjectInstance
        {
            ElementId = GenerateElementId(movement),
            DisplayName = $"Movement of Material {movement.MaterialId}",
            TypeId = MesObjectTypes.MaterialType, // Reuse
            IsComposition = false,
            IsExtended = false
        };
    }

    public ObjectInstance ToI3XObject(InspectionItem item)
    {
        return new ObjectInstance
        {
            ElementId = GenerateElementId(item),
            DisplayName = $"Item: {item.Characteristic}",
            TypeId = MesObjectTypes.InspectionType, // Reuse
            ParentId = item.InspectionId.ToString(),
            IsComposition = true,
            IsExtended = false
        };
    }

    public ObjectInstance ToI3XObject(NonConformance ncr)
    {
        return new ObjectInstance
        {
            ElementId = GenerateElementId(ncr),
            DisplayName = $"NCR: {ncr.Description}",
            TypeId = MesObjectTypes.InspectionType, // Reuse
            ParentId = ncr.WorkOrderId?.ToString(),
            IsComposition = false,
            IsExtended = false
        };
    }

    public object ToI3XValue(WorkOrderOperation operation)
    {
        return new
        {
            id = operation.Id,
            workOrderId = operation.WorkOrderId,
            sequenceNumber = operation.SequenceNumber,
            operationName = operation.OperationName,
            description = operation.Description,
            workCenterId = operation.WorkCenterId,
            workCenterName = operation.WorkCenterName,
            equipmentId = operation.EquipmentId,
            equipmentName = operation.EquipmentName,
            standardCycleTimeMinutes = operation.StandardCycleTimeMinutes,
            quantityPlanned = operation.QuantityPlanned,
            quantityCompleted = operation.QuantityCompleted,
            startDate = operation.StartDate?.ToString("o"),
            endDate = operation.EndDate?.ToString("o"),
            status = operation.Status,
            createdAt = operation.CreatedAt.ToString("o"),
            updatedAt = operation.UpdatedAt?.ToString("o")
        };
    }

    public object ToI3XValue(EquipmentMaintenance maintenance)
    {
        return new
        {
            id = maintenance.Id,
            equipmentId = maintenance.EquipmentId,
            maintenanceType = maintenance.MaintenanceType,
            scheduledDate = maintenance.ScheduledDate.ToString("o"),
            completedDate = maintenance.CompletedDate?.ToString("o"),
            description = maintenance.Description,
            performedBy = maintenance.PerformedBy,
            cost = maintenance.Cost,
            status = maintenance.Status,
            upkeepWorkOrderId = maintenance.UpkeepWorkOrderId,
            createdAt = maintenance.CreatedAt.ToString("o"),
            updatedAt = maintenance.UpdatedAt?.ToString("o")
        };
    }

    public object ToI3XValue(BillOfMaterials bom)
    {
        return new
        {
            id = bom.Id,
            productId = bom.ProductId,
            componentMaterialId = bom.ComponentMaterialId,
            quantityRequired = bom.QuantityRequired,
            notes = bom.Notes,
            createdAt = bom.CreatedAt.ToString("o"),
            updatedAt = bom.UpdatedAt?.ToString("o")
        };
    }

    public object ToI3XValue(MaterialMovement movement)
    {
        return new
        {
            id = movement.Id,
            materialId = movement.MaterialId,
            movementType = movement.MovementType,
            quantity = movement.Quantity,
            fromLocation = movement.FromLocation,
            toLocation = movement.ToLocation,
            workOrderId = movement.WorkOrderId,
            referenceNumber = movement.ReferenceNumber,
            performedBy = movement.PerformedBy,
            movementDate = movement.MovementDate.ToString("o"),
            createdAt = movement.CreatedAt.ToString("o"),
            updatedAt = movement.UpdatedAt?.ToString("o")
        };
    }

    public object ToI3XValue(InspectionItem item)
    {
        return new
        {
            id = item.Id,
            inspectionId = item.InspectionId,
            characteristic = item.Characteristic,
            specification = item.Specification,
            measurement = item.Measurement,
            status = item.Status,
            notes = item.Notes,
            createdAt = item.CreatedAt.ToString("o"),
            updatedAt = item.UpdatedAt?.ToString("o")
        };
    }

    public object ToI3XValue(NonConformance ncr)
    {
        return new
        {
            id = ncr.Id,
            workOrderId = ncr.WorkOrderId,
            inspectionId = ncr.InspectionId,
            equipmentId = ncr.EquipmentId,
            operatorId = ncr.OperatorId,
            discoveredBy = ncr.DiscoveredBy,
            discoveredDate = ncr.DiscoveredDate.ToString("o"),
            type = ncr.Type,
            severity = ncr.Severity,
            description = ncr.Description,
            status = ncr.Status,
            rootCause = ncr.RootCause,
            correctiveAction = ncr.CorrectiveAction,
            closedDate = ncr.ClosedDate?.ToString("o"),
            createdAt = ncr.CreatedAt.ToString("o"),
            updatedAt = ncr.UpdatedAt?.ToString("o")
        };
    }

    public void UpdateFromI3X(WorkOrderOperation operation, object value)
    {
        if (value is JsonElement json) MapProperties(json, operation);
    }

    public void UpdateFromI3X(EquipmentMaintenance maintenance, object value)
    {
        if (value is JsonElement json) MapProperties(json, maintenance);
    }

    public void UpdateFromI3X(BillOfMaterials bom, object value)
    {
        if (value is JsonElement json) MapProperties(json, bom);
    }

    public void UpdateFromI3X(MaterialMovement movement, object value)
    {
        if (value is JsonElement json) MapProperties(json, movement);
    }

    public void UpdateFromI3X(InspectionItem item, object value)
    {
        if (value is JsonElement json) MapProperties(json, item);
    }

    public void UpdateFromI3X(NonConformance ncr, object value)
    {
        if (value is JsonElement json) MapProperties(json, ncr);
    }

    // Update entities from i3X value (dictionary/JSON)
    public void UpdateFromI3X(WorkOrder workOrder, object value)
    {
        if (value is JsonElement json)
        {
            MapProperties(json, workOrder);
        }
    }

    public void UpdateFromI3X(Equipment equipment, object value)
    {
        if (value is JsonElement json)
        {
            MapProperties(json, equipment);
        }
    }

    public void UpdateFromI3X(Material material, object value)
    {
        if (value is JsonElement json)
        {
            MapProperties(json, material);
        }
    }

    public void UpdateFromI3X(Operator op, object value)
    {
        if (value is JsonElement json)
        {
            MapProperties(json, op);
        }
    }

    public void UpdateFromI3X(Inspection inspection, object value)
    {
        if (value is JsonElement json)
        {
            MapProperties(json, inspection);
        }
    }

    public void UpdateFromI3X(Shift shift, object value)
    {
        if (value is JsonElement json)
        {
            MapProperties(json, shift);
        }
    }

    public void UpdateFromI3X(LaborRecord laborRecord, object value)
    {
        if (value is JsonElement json)
        {
            MapProperties(json, laborRecord);
        }
    }

    // Helper to map JSON properties to entity properties using reflection or manual mapping
    private void MapProperties(JsonElement json, object target)
    {
        var type = target.GetType();
        foreach (var prop in json.EnumerateObject())
        {
            var propertyInfo = type.GetProperty(ToPascalCase(prop.Name));
            if (propertyInfo == null) continue;

            try
            {
                var value = ConvertJsonValue(prop.Value, propertyInfo.PropertyType);
                if (value != null)
                {
                    propertyInfo.SetValue(target, value);
                }
            }
            catch
            {
                // Skip properties that can't be converted
            }
        }
    }

    private string ToPascalCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        return char.ToUpper(name[0]) + name.Substring(1);
    }

    private static object? ConvertJsonValue(JsonElement element, Type targetType)
    {
        if (element.ValueKind == JsonValueKind.Null || element.ValueKind == JsonValueKind.Undefined)
        {
            return null;
        }

        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (underlyingType == typeof(string)) return element.GetString();
        if (underlyingType == typeof(int)) return element.GetInt32();
        if (underlyingType == typeof(long)) return element.GetInt64();
        if (underlyingType == typeof(decimal)) return element.GetDecimal();
        if (underlyingType == typeof(double)) return element.GetDouble();
        if (underlyingType == typeof(float)) return element.GetSingle();
        if (underlyingType == typeof(bool)) return element.GetBoolean();
        if (underlyingType == typeof(DateTime)) return element.GetDateTime();
        if (underlyingType == typeof(TimeSpan)) return TimeSpan.Parse(element.GetString() ?? "00:00:00");

        if (underlyingType.IsEnum)
        {
            return element.ValueKind == JsonValueKind.Number
                ? Enum.ToObject(underlyingType, element.GetInt32())
                : Enum.Parse(underlyingType, element.GetString() ?? string.Empty, ignoreCase: true);
        }

        return JsonSerializer.Deserialize(element.GetRawText(), underlyingType);
    }

    public string GenerateElementId<T>(T entity) where T : Entity
    {
        var type = typeof(T);
        var prefix = _elementIdPrefixes.TryGetValue(type, out var p) ? p : "obj";
        return $"{prefix}-{entity.Id}";
    }
}
