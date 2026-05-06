namespace CoroMES.Core.Entities;

public class Operator : Entity
{
    public string EmployeeNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Department { get; set; }
    public string? Role { get; set; } = "Operator"; // Operator, Supervisor, Technician
    public int? ShiftId { get; set; }
    public int? UpkeepUserId { get; set; }

    public Shift? Shift { get; set; }
    public ICollection<LaborRecord> LaborRecords { get; set; } = new List<LaborRecord>();
}

public class Shift : Entity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public int? ProductionLineId { get; set; }
    public string? ProductionLineName { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Operator> Operators { get; set; } = new List<Operator>();
    public ICollection<LaborRecord> LaborRecords { get; set; } = new List<LaborRecord>();
}

public class LaborRecord : Entity
{
    public int OperatorId { get; set; }
    public int? ShiftId { get; set; }
    public int? WorkOrderId { get; set; }
    public int? EquipmentId { get; set; }
    public DateTime WorkDate { get; set; }
    public decimal HoursWorked { get; set; }
    public string? ActivityCode { get; set; } // Production, Setup, Maintenance, Break
    public string? Notes { get; set; }

    public Operator? Operator { get; set; }
    public Shift? Shift { get; set; }
    public WorkOrder? WorkOrder { get; set; }
    public Equipment? Equipment { get; set; }
}