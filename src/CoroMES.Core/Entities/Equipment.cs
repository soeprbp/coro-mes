using CoroMES.Core.Enums;

namespace CoroMES.Core.Entities;

public class Equipment : Entity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? SerialNumber { get; set; }
    public string? Model { get; set; }
    public string? Manufacturer { get; set; }
    public EquipmentType Type { get; set; } = EquipmentType.Machine;
    public EquipmentStatus Status { get; set; } = EquipmentStatus.Available;
    public int? ProductionLineId { get; set; }
    public string? ProductionLineName { get; set; }
    public string? Location { get; set; }
    public string? IpAddress { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastMaintenanceDate { get; set; }
    public int? UpkeepAssetId { get; set; } // Reference to Upkeep CMMS

    // Production capabilities
    public int? PartsPerMinute { get; set; }
    public int? SqFtPerDay { get; set; }
    public int? CycleTimeSeconds { get; set; }

    // Protocol configuration
    public string? Protocol { get; set; } // auto, mqtt, opcua, ethernetip, manual

    public ICollection<EquipmentMaintenance> Maintenances { get; set; } = new List<EquipmentMaintenance>();
}

public class EquipmentMaintenance : Entity
{
    public int EquipmentId { get; set; }
    public string MaintenanceType { get; set; } = string.Empty; // Preventive, Corrective, Predictive
    public DateTime ScheduledDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public string? Description { get; set; }
    public string? PerformedBy { get; set; }
    public decimal? Cost { get; set; }
    public string Status { get; set; } = "Scheduled"; // Scheduled, InProgress, Completed
    public int? UpkeepWorkOrderId { get; set; }

    public Equipment? Equipment { get; set; }
}