namespace CoroMES.Core.Entities;

public class Inspection : Entity
{
    public int? WorkOrderId { get; set; }
    public int? EquipmentId { get; set; }
    public int? OperatorId { get; set; }
    public string InspectionType { get; set; } = string.Empty; // In-Process, Final, Receiving
    public DateTime InspectionDate { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, InProgress, Completed
    public int QuantityInspected { get; set; }
    public int QuantityPassed { get; set; }
    public int QuantityFailed { get; set; }
    public string? Result { get; set; } // Pass, Fail, Conditional
    public string? Notes { get; set; }

    public WorkOrder? WorkOrder { get; set; }
    public Equipment? Equipment { get; set; }
    public Operator? Operator { get; set; }
    public ICollection<InspectionItem> Items { get; set; } = new List<InspectionItem>();
}

public class InspectionItem : Entity
{
    public int InspectionId { get; set; }
    public string Characteristic { get; set; } = string.Empty;
    public string Specification { get; set; } = string.Empty;
    public string? Measurement { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Pass, Fail
    public string? Notes { get; set; }

    public Inspection? Inspection { get; set; }
}

public class NonConformance : Entity
{
    public int? WorkOrderId { get; set; }
    public int? InspectionId { get; set; }
    public int? EquipmentId { get; set; }
    public int? OperatorId { get; set; }
    public string? DiscoveredBy { get; set; }
    public DateTime DiscoveredDate { get; set; }
    public string Type { get; set; } = string.Empty; // Material, Process, Product
    public string Severity { get; set; } = string.Empty; // Minor, Major, Critical
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = "Open"; // Open, Investigating, CorrectiveAction, Closed
    public string? RootCause { get; set; }
    public string? CorrectiveAction { get; set; }
    public DateTime? ClosedDate { get; set; }

    public WorkOrder? WorkOrder { get; set; }
    public Inspection? Inspection { get; set; }
    public Equipment? Equipment { get; set; }
}