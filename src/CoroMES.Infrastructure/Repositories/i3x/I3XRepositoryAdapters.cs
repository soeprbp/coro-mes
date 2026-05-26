using CoroMES.Core.Entities;
using CoroMES.Core.Enums;
using CoroMES.Core.Interfaces.Repositories;
using CoroMES.Industrial.i3X;
using CoroMES.Industrial.i3X.Translators;
using Microsoft.Extensions.Logging;

namespace CoroMES.Infrastructure.Repositories.i3x;

public class I3XWorkOrderRepository : I3XRepository<WorkOrder>, IWorkOrderRepository
{
    public I3XWorkOrderRepository(II3XClient client, IMesEntityTranslator translator, ILogger<I3XRepository<WorkOrder>> logger)
        : base(client, translator, logger) { }

    public async Task<IEnumerable<WorkOrder>> GetByStatusAsync(WorkOrderStatus status) =>
        (await GetAllAsync()).Where(w => w.Status == status);

    public async Task<IEnumerable<WorkOrder>> GetByOperatorAsync(int operatorId) =>
        (await GetAllAsync()).Where(w => w.OperatorId == operatorId);

    public async Task<IEnumerable<WorkOrder>> GetActiveAsync() =>
        (await GetAllAsync()).Where(w => w.Status is WorkOrderStatus.Planned or WorkOrderStatus.InProgress);

    public async Task<IEnumerable<WorkOrder>> GetByProductionLineAsync(string productionLineName) =>
        (await GetAllAsync()).Where(w => w.ProductionLineName == productionLineName);

    public async Task<IEnumerable<WorkOrder>> GetByDateRangeAsync(DateTime startDate, DateTime endDate) =>
        (await GetAllAsync()).Where(w => w.ScheduledStartDate >= startDate && w.ScheduledStartDate <= endDate);
}

public class I3XWorkOrderOperationRepository : I3XRepository<WorkOrderOperation>, IWorkOrderOperationRepository
{
    public I3XWorkOrderOperationRepository(II3XClient client, IMesEntityTranslator translator, ILogger<I3XRepository<WorkOrderOperation>> logger)
        : base(client, translator, logger) { }

    public async Task<IEnumerable<WorkOrderOperation>> GetByWorkOrderIdAsync(int workOrderId) =>
        (await GetAllAsync()).Where(o => o.WorkOrderId == workOrderId).OrderBy(o => o.SequenceNumber);

    public async Task<IEnumerable<WorkOrderOperation>> GetByEquipmentAsync(int equipmentId) =>
        (await GetAllAsync()).Where(o => o.EquipmentId == equipmentId);

    public async Task<WorkOrderOperation?> GetByWorkOrderAndSequenceAsync(int workOrderId, int sequenceNumber) =>
        (await GetAllAsync()).FirstOrDefault(o => o.WorkOrderId == workOrderId && o.SequenceNumber == sequenceNumber);
}

public class I3XEquipmentRepository : I3XRepository<Equipment>, IEquipmentRepository
{
    public I3XEquipmentRepository(II3XClient client, IMesEntityTranslator translator, ILogger<I3XRepository<Equipment>> logger)
        : base(client, translator, logger) { }

    public async Task<Equipment?> GetByCodeAsync(string code) =>
        (await GetAllAsync()).FirstOrDefault(e => e.Code == code);

    public async Task<Equipment?> GetByUpkeepAssetIdAsync(int upkeepAssetId) =>
        (await GetAllAsync()).FirstOrDefault(e => e.UpkeepAssetId == upkeepAssetId);

    public async Task<IEnumerable<Equipment>> GetByProtocolAsync(string protocol) =>
        (await GetAllAsync()).Where(e => e.Protocol == protocol);

    public async Task<IEnumerable<Equipment>> GetByStatusAsync(EquipmentStatus status) =>
        (await GetAllAsync()).Where(e => e.Status == status);

    public async Task<IEnumerable<Equipment>> GetWithRecentMaintenanceAsync(DateTime since) =>
        await GetAllAsync();
}

public class I3XEquipmentMaintenanceRepository : I3XRepository<EquipmentMaintenance>, IEquipmentMaintenanceRepository
{
    public I3XEquipmentMaintenanceRepository(II3XClient client, IMesEntityTranslator translator, ILogger<I3XRepository<EquipmentMaintenance>> logger)
        : base(client, translator, logger) { }

    public async Task<IEnumerable<EquipmentMaintenance>> GetByEquipmentIdAsync(int equipmentId) =>
        (await GetAllAsync()).Where(m => m.EquipmentId == equipmentId).OrderByDescending(m => m.ScheduledDate);

    public async Task<IEnumerable<EquipmentMaintenance>> GetByTypeAsync(string maintenanceType) =>
        (await GetAllAsync()).Where(m => m.MaintenanceType == maintenanceType);

    public async Task<IEnumerable<EquipmentMaintenance>> GetOverdueAsync(DateTime asOf) =>
        (await GetAllAsync()).Where(m => m.Status == "Scheduled" && m.ScheduledDate < asOf);

    public async Task<IEnumerable<EquipmentMaintenance>> GetHistoryAsync(int equipmentId, DateTime startDate, DateTime endDate) =>
        (await GetAllAsync()).Where(m => m.EquipmentId == equipmentId && m.ScheduledDate >= startDate && m.ScheduledDate <= endDate)
                            .OrderByDescending(m => m.ScheduledDate);
}

public class I3XMaterialRepository : I3XRepository<Material>, IMaterialRepository
{
    public I3XMaterialRepository(II3XClient client, IMesEntityTranslator translator, ILogger<I3XRepository<Material>> logger)
        : base(client, translator, logger) { }

    public async Task<Material?> GetByCodeAsync(string code) =>
        (await GetAllAsync()).FirstOrDefault(m => m.Code == code);

    public async Task<IEnumerable<Material>> GetLowInventoryAsync(int threshold) =>
        (await GetAllAsync()).Where(m => m.CurrentQuantity < threshold);

    public async Task<IEnumerable<Material>> GetByTypeAsync(string materialType) =>
        (await GetAllAsync()).Where(m => m.Category == materialType);

    public Task<IEnumerable<Material>> GetByBillOfMaterialsAsync(int bomId) =>
        Task.FromResult(Enumerable.Empty<Material>());
}

public class I3XBillOfMaterialsRepository : I3XRepository<BillOfMaterials>, IBillOfMaterialsRepository
{
    public I3XBillOfMaterialsRepository(II3XClient client, IMesEntityTranslator translator, ILogger<I3XRepository<BillOfMaterials>> logger)
        : base(client, translator, logger) { }

    public async Task<IEnumerable<BillOfMaterials>> GetByProductIdAsync(int productId) =>
        (await GetAllAsync()).Where(b => b.ProductId == productId);

    public async Task<IEnumerable<BillOfMaterials>> GetByMaterialAsync(int materialId) =>
        (await GetAllAsync()).Where(b => b.ComponentMaterialId == materialId);

    public Task<BillOfMaterials?> GetWithItemsAsync(int bomId) => GetByIdAsync(bomId);
}

public class I3XMaterialMovementRepository : I3XRepository<MaterialMovement>, IMaterialMovementRepository
{
    public I3XMaterialMovementRepository(II3XClient client, IMesEntityTranslator translator, ILogger<I3XRepository<MaterialMovement>> logger)
        : base(client, translator, logger) { }

    public async Task<IEnumerable<MaterialMovement>> GetByMaterialAsync(int materialId) =>
        (await GetAllAsync()).Where(m => m.MaterialId == materialId).OrderByDescending(m => m.MovementDate);

    public async Task<IEnumerable<MaterialMovement>> GetByTypeAsync(string movementType) =>
        (await GetAllAsync()).Where(m => m.MovementType == movementType);

    public async Task<IEnumerable<MaterialMovement>> GetByDateRangeAsync(DateTime startDate, DateTime endDate) =>
        (await GetAllAsync()).Where(m => m.MovementDate >= startDate && m.MovementDate <= endDate).OrderByDescending(m => m.MovementDate);

    public async Task<IEnumerable<MaterialMovement>> GetRecentAsync(int days)
    {
        var since = DateTime.UtcNow.AddDays(-days);
        return (await GetAllAsync()).Where(m => m.MovementDate >= since).OrderByDescending(m => m.MovementDate);
    }
}

public class I3XOperatorRepository : I3XRepository<Operator>, IOperatorRepository
{
    public I3XOperatorRepository(II3XClient client, IMesEntityTranslator translator, ILogger<I3XRepository<Operator>> logger)
        : base(client, translator, logger) { }

    public async Task<Operator?> GetByEmployeeNumberAsync(string employeeNumber) =>
        (await GetAllAsync()).FirstOrDefault(o => o.EmployeeNumber == employeeNumber);

    public async Task<IEnumerable<Operator>> GetByShiftAsync(int shiftId) =>
        (await GetAllAsync()).Where(o => o.ShiftId == shiftId);

    public async Task<IEnumerable<Operator>> GetOnDutyAsync(DateTime asOf) =>
        (await GetAllAsync()).Where(o => o.IsActive);

    public Task<Operator?> GetWithLaborRecordsAsync(int operatorId) => GetByIdAsync(operatorId);
}

public class I3XShiftRepository : I3XRepository<Shift>, IShiftRepository
{
    public I3XShiftRepository(II3XClient client, IMesEntityTranslator translator, ILogger<I3XRepository<Shift>> logger)
        : base(client, translator, logger) { }

    public async Task<Shift?> GetByCodeAsync(string code) =>
        (await GetAllAsync()).FirstOrDefault(s => s.Code == code);

    public async Task<IEnumerable<Shift>> GetActiveAsync(DateTime asOf) =>
        (await GetAllAsync()).Where(s => s.IsActive && s.StartTime <= asOf.TimeOfDay && s.EndTime >= asOf.TimeOfDay);

    public Task<Shift?> GetWithOperatorsAsync(int shiftId) => GetByIdAsync(shiftId);
}

public class I3XLaborRecordRepository : I3XRepository<LaborRecord>, ILaborRecordRepository
{
    public I3XLaborRecordRepository(II3XClient client, IMesEntityTranslator translator, ILogger<I3XRepository<LaborRecord>> logger)
        : base(client, translator, logger) { }

    public async Task<IEnumerable<LaborRecord>> GetByOperatorAsync(int operatorId) =>
        (await GetAllAsync()).Where(l => l.OperatorId == operatorId).OrderByDescending(l => l.WorkDate);

    public async Task<IEnumerable<LaborRecord>> GetByWorkOrderAsync(int workOrderId) =>
        (await GetAllAsync()).Where(l => l.WorkOrderId == workOrderId);

    public async Task<IEnumerable<LaborRecord>> GetByDateRangeAsync(DateTime startDate, DateTime endDate) =>
        (await GetAllAsync()).Where(l => l.WorkDate >= startDate && l.WorkDate <= endDate).OrderByDescending(l => l.WorkDate);

    public async Task<decimal> GetTotalHoursByOperatorAsync(int operatorId, DateTime date)
    {
        var start = date.Date;
        var end = start.AddDays(1);
        return (await GetAllAsync()).Where(l => l.OperatorId == operatorId && l.WorkDate >= start && l.WorkDate < end).Sum(l => l.HoursWorked);
    }
}

public class I3XInspectionRepository : I3XRepository<Inspection>, IInspectionRepository
{
    public I3XInspectionRepository(II3XClient client, IMesEntityTranslator translator, ILogger<I3XRepository<Inspection>> logger)
        : base(client, translator, logger) { }

    public async Task<IEnumerable<Inspection>> GetByWorkOrderAsync(int workOrderId) =>
        (await GetAllAsync()).Where(i => i.WorkOrderId == workOrderId).OrderByDescending(i => i.InspectionDate);

    public async Task<IEnumerable<Inspection>> GetByOperatorAsync(int operatorId) =>
        (await GetAllAsync()).Where(i => i.OperatorId == operatorId);

    public async Task<IEnumerable<Inspection>> GetByStatusAsync(string status) =>
        (await GetAllAsync()).Where(i => i.Status == status);

    public async Task<IEnumerable<Inspection>> GetByDateRangeAsync(DateTime startDate, DateTime endDate) =>
        (await GetAllAsync()).Where(i => i.InspectionDate >= startDate && i.InspectionDate <= endDate).OrderByDescending(i => i.InspectionDate);

    public Task<Inspection?> GetWithItemsAsync(int inspectionId) => GetByIdAsync(inspectionId);
}

public class I3XInspectionItemRepository : I3XRepository<InspectionItem>, IInspectionItemRepository
{
    public I3XInspectionItemRepository(II3XClient client, IMesEntityTranslator translator, ILogger<I3XRepository<InspectionItem>> logger)
        : base(client, translator, logger) { }

    public async Task<IEnumerable<InspectionItem>> GetByInspectionAsync(int inspectionId) =>
        (await GetAllAsync()).Where(i => i.InspectionId == inspectionId);

    public async Task<IEnumerable<InspectionItem>> GetFailedAsync(int? inspectionId = null)
    {
        var items = (await GetAllAsync()).Where(i => i.Status == "Fail");
        return inspectionId.HasValue ? items.Where(i => i.InspectionId == inspectionId.Value) : items;
    }
}

public class I3XNonConformanceRepository : I3XRepository<NonConformance>, INonConformanceRepository
{
    public I3XNonConformanceRepository(II3XClient client, IMesEntityTranslator translator, ILogger<I3XRepository<NonConformance>> logger)
        : base(client, translator, logger) { }

    public async Task<IEnumerable<NonConformance>> GetByWorkOrderAsync(int workOrderId) =>
        (await GetAllAsync()).Where(n => n.WorkOrderId == workOrderId).OrderByDescending(n => n.CreatedAt);

    public async Task<IEnumerable<NonConformance>> GetByInspectorAsync(int inspectorId) =>
        (await GetAllAsync()).Where(n => n.OperatorId == inspectorId);

    public async Task<IEnumerable<NonConformance>> GetOpenAsync() =>
        (await GetAllAsync()).Where(n => n.Status != "Closed" && n.Status != "Resolved");

    public async Task<IEnumerable<NonConformance>> GetBySeverityAsync(string severity) =>
        (await GetAllAsync()).Where(n => n.Severity == severity);

    public Task<NonConformance?> GetWithInspectionAsync(int ncrId) => GetByIdAsync(ncrId);
}
