namespace CoroMES.Core.Enums;

public enum WorkOrderStatus
{
    Planned = 0,
    InProgress = 1,
    OnHold = 2,
    Completed = 3,
    Cancelled = 4
}

public enum WorkOrderPriority
{
    Low = 0,
    Normal = 1,
    High = 2,
    Urgent = 3
}