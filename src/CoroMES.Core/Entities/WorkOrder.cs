using CoroMES.Core.Enums;

namespace CoroMES.Core.Entities;

public class WorkOrder : Entity
{
    public string Number { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? ProductId { get; set; }
    public string? ProductName { get; set; }
    public int QuantityPlanned { get; set; }
    public int QuantityCompleted { get; set; }
    public int QuantityScrap { get; set; }
    public WorkOrderStatus Status { get; set; } = WorkOrderStatus.Planned;
    public WorkOrderPriority Priority { get; set; } = WorkOrderPriority.Normal;
    public int? ProductionLineId { get; set; }
    public string? ProductionLineName { get; set; }
    public int? OperatorId { get; set; }
    public string? OperatorName { get; set; }
    public DateTime? ScheduledStartDate { get; set; }
    public DateTime? ScheduledEndDate { get; set; }
    public DateTime? ActualStartDate { get; set; }
    public DateTime? ActualEndDate { get; set; }
    public string? CustomerOrderNumber { get; set; }
    public string? Notes { get; set; }

    public ICollection<WorkOrderOperation> Operations { get; set; } = new List<WorkOrderOperation>();
}

public class WorkOrderOperation : Entity
{
    public int WorkOrderId { get; set; }
    public int SequenceNumber { get; set; }
    public string OperationName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? WorkCenterId { get; set; }
    public string? WorkCenterName { get; set; }
    public int? EquipmentId { get; set; }
    public string? EquipmentName { get; set; }
    public int StandardCycleTimeMinutes { get; set; }
    public int QuantityPlanned { get; set; }
    public int QuantityCompleted { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, InProgress, Completed

    public WorkOrder? WorkOrder { get; set; }
}