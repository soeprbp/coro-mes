namespace CoroMES.Core.Enums;

public enum EquipmentStatus
{
    Available = 0,
    Running = 1,
    Idle = 2,
    Down = 3,
    Maintenance = 4,
    Setup = 5
}

public enum EquipmentType
{
    Machine = 0,
    Workstation = 1,
    Conveyor = 2,
    Robot = 3,
    Sensor = 4,
    Other = 99
}