using System.Text.Json;
using CoroMES.Industrial.i3X.Models;

namespace CoroMES.Industrial.i3X.ObjectTypes;

/// <summary>
/// Defines i3X object type metadata for all MES entities.
/// </summary>
public static class MesObjectTypes
{
    public const string NamespaceUri = "https://coromes.com/ns/mes";

    // Type ElementIds
    public const string WorkOrderType = "mes:WorkOrder";
    public const string EquipmentType = "mes:Equipment";
    public const string MaterialType = "mes:Material";
    public const string OperatorType = "mes:Operator";
    public const string InspectionType = "mes:Inspection";
    public const string ShiftType = "mes:Shift";
    public const string LaborRecordType = "mes:LaborRecord";

    public static IEnumerable<ObjectType> GetAll()
    {
        return new[]
        {
            CreateWorkOrderType(),
            CreateEquipmentType(),
            CreateMaterialType(),
            CreateOperatorType(),
            CreateInspectionType(),
            CreateShiftType(),
            CreateLaborRecordType()
        };
    }

    private static ObjectType CreateWorkOrderType()
    {
        return new ObjectType
        {
            ElementId = WorkOrderType,
            DisplayName = "Work Order",
            NamespaceUri = NamespaceUri,
            SourceTypeId = "WorkOrder",
            Version = "1.0.0",
            Schema = JsonSerializer.Deserialize<object>(@"{
                ""type"": ""object"",
                ""properties"": {
                    ""id"": { ""type"": ""integer"" },
                    ""number"": { ""type"": ""string"" },
                    ""description"": { ""type"": [""string"", ""null""] },
                    ""status"": { ""type"": ""string"", ""enum"": [""Planned"", ""Released"", ""InProduction"", ""OnHold"", ""Completed"", ""Cancelled""] },
                    ""priority"": { ""type"": ""string"", ""enum"": [""Low"", ""Medium"", ""High"", ""Critical""] },
                    ""quantityPlanned"": { ""type"": ""number"" },
                    ""quantityCompleted"": { ""type"": ""number"" },
                    ""quantityScrap"": { ""type"": ""number"" },
                    ""productId"": { ""type"": ""integer"" },
                    ""productName"": { ""type"": [""string"", ""null""] },
                    ""productionLineId"": { ""type"": ""integer"" },
                    ""productionLineName"": { ""type"": [""string"", ""null""] },
                    ""operatorId"": { ""type"": [""integer"", ""null""] },
                    ""operatorName"": { ""type"": [""string"", ""null""] },
                    ""scheduledStartDate"": { ""type"": [""string"", ""null""], ""format"": ""date-time"" },
                    ""scheduledEndDate"": { ""type"": [""string"", ""null""], ""format"": ""date-time"" },
                    ""actualStartDate"": { ""type"": [""string"", ""null""], ""format"": ""date-time"" },
                    ""actualEndDate"": { ""type"": [""string"", ""null""], ""format"": ""date-time"" },
                    ""customerOrderNumber"": { ""type"": [""string"", ""null""] },
                    ""notes"": { ""type"": [""string"", ""null""] },
                    ""createdAt"": { ""type"": ""string"", ""format"": ""date-time"" },
                    ""updatedAt"": { ""type"": [""string"", ""null""], ""format"": ""date-time"" }
                },
                ""required"": [""id"", ""number"", ""status""]
            }")!
        };
    }

    private static ObjectType CreateEquipmentType()
    {
        return new ObjectType
        {
            ElementId = EquipmentType,
            DisplayName = "Equipment / Asset",
            NamespaceUri = NamespaceUri,
            SourceTypeId = "Equipment",
            Version = "1.0.0",
            Schema = JsonSerializer.Deserialize<object>(@"{
                ""type"": ""object"",
                ""properties"": {
                    ""id"": { ""type"": ""integer"" },
                    ""code"": { ""type"": ""string"" },
                    ""name"": { ""type"": ""string"" },
                    ""description"": { ""type"": [""string"", ""null""] },
                    ""type"": { ""type"": ""string"", ""enum"": [""Machine"", ""Sensor"", ""Fixture"", ""Tool"", ""Other""] },
                    ""status"": { ""type"": ""string"", ""enum"": [""Available"", ""Running"", ""Idle"", ""Down"", ""Maintenance""] },
                    ""serialNumber"": { ""type"": [""string"", ""null""] },
                    ""model"": { ""type"": [""string"", ""null""] },
                    ""manufacturer"": { ""type"": [""string"", ""null""] },
                    ""location"": { ""type"": [""string"", ""null""] },
                    ""ipAddress"": { ""type"": [""string"", ""null""] },
                    ""isActive"": { ""type"": ""boolean"" },
                    ""lastMaintenanceDate"": { ""type"": [""string"", ""null""], ""format"": ""date-time"" },
                    ""upkeepAssetId"": { ""type"": [""integer"", ""null""] },
                    ""partsPerMinute"": { ""type"": [""integer"", ""null""] },
                    ""sqFtPerDay"": { ""type"": [""integer"", ""null""] },
                    ""cycleTimeSeconds"": { ""type"": [""integer"", ""null""] },
                    ""protocol"": { ""type"": [""string"", ""null""], ""enum"": [""mqtt"", ""opcua"", ""ethernetip"", ""manual"", ""auto""] },
                    ""createdAt"": { ""type"": ""string"", ""format"": ""date-time"" },
                    ""updatedAt"": { ""type"": [""string"", ""null""], ""format"": ""date-time"" }
                },
                ""required"": [""id"", ""code"", ""name"", ""type"", ""status""]
            }")!
        };
    }

    private static ObjectType CreateMaterialType()
    {
        return new ObjectType
        {
            ElementId = MaterialType,
            DisplayName = "Material / Inventory Item",
            NamespaceUri = NamespaceUri,
            SourceTypeId = "Material",
            Version = "1.0.0",
            Schema = JsonSerializer.Deserialize<object>(@"{
                ""type"": ""object"",
                ""properties"": {
                    ""id"": { ""type"": ""integer"" },
                    ""code"": { ""type"": ""string"" },
                    ""name"": { ""type"": ""string"" },
                    ""description"": { ""type"": [""string"", ""null""] },
                    ""unitOfMeasure"": { ""type"": ""string"", ""enum"": [""EA"", ""LB"", ""KG"", ""FT"", ""M"", ""L"", ""GAL""] },
                    ""unitCost"": { ""type"": [""number"", ""null""] },
                    ""category"": { ""type"": ""string"", ""enum"": [""Raw Material"", ""Component"", ""Finished Good"", ""Supply"", ""WIP""] },
                    ""currentQuantity"": { ""type"": ""number"" },
                    ""minimumQuantity"": { ""type"": ""number"" },
                    ""maximumQuantity"": { ""type"": [""number"", ""null""] },
                    ""location"": { ""type"": [""string"", ""null""] },
                    ""isActive"": { ""type"": ""boolean"" },
                    ""supplierId"": { ""type"": [""integer"", ""null""] },
                    ""supplierName"": { ""type"": [""string"", ""null""] },
                    ""createdAt"": { ""type"": ""string"", ""format"": ""date-time"" },
                    ""updatedAt"": { ""type"": [""string"", ""null""], ""format"": ""date-time"" }
                },
                ""required"": [""id"", ""code"", ""name"", ""unitOfMeasure"", ""currentQuantity""]
            }")!
        };
    }

    private static ObjectType CreateOperatorType()
    {
        return new ObjectType
        {
            ElementId = OperatorType,
            DisplayName = "Operator / User",
            NamespaceUri = NamespaceUri,
            SourceTypeId = "Operator",
            Version = "1.0.0",
            Schema = JsonSerializer.Deserialize<object>(@"{
                ""type"": ""object"",
                ""properties"": {
                    ""id"": { ""type"": ""integer"" },
                    ""employeeNumber"": { ""type"": ""string"" },
                    ""firstName"": { ""type"": ""string"" },
                    ""lastName"": { ""type"": ""string"" },
                    ""email"": { ""type"": [""string"", ""null""] },
                    ""phone"": { ""type"": [""string"", ""null""] },
                    ""isActive"": { ""type"": ""boolean"" },
                    ""department"": { ""type"": [""string"", ""null""] },
                    ""role"": { ""type"": ""string"", ""enum"": [""Operator"", ""Supervisor"", ""Technician"", ""Quality"", ""Manager""] },
                    ""shiftId"": { ""type"": [""integer"", ""null""] },
                    ""upkeepUserId"": { ""type"": [""integer"", ""null""] },
                    ""createdAt"": { ""type"": ""string"", ""format"": ""date-time"" },
                    ""updatedAt"": { ""type"": [""string"", ""null""], ""format"": ""date-time"" }
                },
                ""required"": [""id"", ""employeeNumber"", ""firstName"", ""lastName"", ""role""]
            }")!
        };
    }

    private static ObjectType CreateInspectionType()
    {
        return new ObjectType
        {
            ElementId = InspectionType,
            DisplayName = "Quality Inspection",
            NamespaceUri = NamespaceUri,
            SourceTypeId = "Inspection",
            Version = "1.0.0",
            Schema = JsonSerializer.Deserialize<object>(@"{
                ""type"": ""object"",
                ""properties"": {
                    ""id"": { ""type"": ""integer"" },
                    ""workOrderId"": { ""type"": [""integer"", ""null""] },
                    ""equipmentId"": { ""type"": [""integer"", ""null""] },
                    ""operatorId"": { ""type"": [""integer"", ""null""] },
                    ""inspectionType"": { ""type"": ""string"", ""enum"": [""In-Process"", ""Final"", ""Receiving"", ""FirstArticle"", ""Incoming""] },
                    ""inspectionDate"": { ""type"": ""string"", ""format"": ""date-time"" },
                    ""status"": { ""type"": ""string"", ""enum"": [""Pending"", ""InProgress"", ""Completed"", ""Rejected""] },
                    ""quantityInspected"": { ""type"": ""integer"" },
                    ""quantityPassed"": { ""type"": ""integer"" },
                    ""quantityFailed"": { ""type"": ""integer"" },
                    ""result"": { ""type"": [""string"", ""null""], ""enum"": [""Pass"", ""Fail"", ""Conditional""] },
                    ""notes"": { ""type"": [""string"", ""null""] },
                    ""createdAt"": { ""type"": ""string"", ""format"": ""date-time"" },
                    ""updatedAt"": { ""type"": [""string"", ""null""], ""format"": ""date-time"" }
                },
                ""required"": [""id"", ""inspectionType"", ""status""]
            }")!
        };
    }

    private static ObjectType CreateShiftType()
    {
        return new ObjectType
        {
            ElementId = ShiftType,
            DisplayName = "Shift / Schedule",
            NamespaceUri = NamespaceUri,
            SourceTypeId = "Shift",
            Version = "1.0.0",
            Schema = JsonSerializer.Deserialize<object>(@"{
                ""type"": ""object"",
                ""properties"": {
                    ""id"": { ""type"": ""integer"" },
                    ""code"": { ""type"": ""string"" },
                    ""name"": { ""type"": ""string"" },
                    ""startTime"": { ""type"": ""string"", ""format"": ""time"" },
                    ""endTime"": { ""type"": ""string"", ""format"": ""time"" },
                    ""productionLineId"": { ""type"": [""string"", ""null""] },
                    ""productionLineName"": { ""type"": [""string"", ""null""] },
                    ""isActive"": { ""type"": ""boolean"" },
                    ""createdAt"": { ""type"": ""string"", ""format"": ""date-time"" },
                    ""updatedAt"": { ""type"": [""string"", ""null""], ""format"": ""date-time"" }
                },
                ""required"": [""id"", ""code"", ""name""]
            }")!
        };
    }

    private static ObjectType CreateLaborRecordType()
    {
        return new ObjectType
        {
            ElementId = LaborRecordType,
            DisplayName = "Labor Record",
            NamespaceUri = NamespaceUri,
            SourceTypeId = "LaborRecord",
            Version = "1.0.0",
            Schema = JsonSerializer.Deserialize<object>(@"{
                ""type"": ""object"",
                ""properties"": {
                    ""id"": { ""type"": ""integer"" },
                    ""operatorId"": { ""type"": ""integer"" },
                    ""shiftId"": { ""type"": [""integer"", ""null""] },
                    ""workOrderId"": { ""type"": [""integer"", ""null""] },
                    ""equipmentId"": { ""type"": [""integer"", ""null""] },
                    ""workDate"": { ""type"": ""string"", ""format"": ""date-time"" },
                    ""hoursWorked"": { ""type"": ""number"" },
                    ""activityCode"": { ""type"": [""string"", ""null""], ""enum"": [""Production"", ""Setup"", ""Maintenance"", ""Break"", ""Training""] },
                    ""notes"": { ""type"": [""string"", ""null""] },
                    ""createdAt"": { ""type"": ""string"", ""format"": ""date-time"" },
                    ""updatedAt"": { ""type"": [""string"", ""null""], ""format"": ""date-time"" }
                },
                ""required"": [""id"", ""operatorId"", ""workDate"", ""hoursWorked""]
            }")!
        };
    }
}
