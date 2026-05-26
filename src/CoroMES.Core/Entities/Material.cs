namespace CoroMES.Core.Entities;

public class Material : Entity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string UnitOfMeasure { get; set; } = "EA"; // EA, LB, KG, FT, etc.
    public decimal? UnitCost { get; set; }
    public string Category { get; set; } = string.Empty; // Raw Material, Component, Finished Good
    public decimal CurrentQuantity { get; set; }
    public decimal MinimumQuantity { get; set; }
    public decimal? MaximumQuantity { get; set; }
    public string? Location { get; set; }
    public bool IsActive { get; set; } = true;
    public int? SupplierId { get; set; }
    public string? SupplierName { get; set; }

    public ICollection<BillOfMaterials> BillOfMaterialsItems { get; set; } = new List<BillOfMaterials>();
    public ICollection<MaterialMovement> MaterialMovements { get; set; } = new List<MaterialMovement>();
}

public class BillOfMaterials : Entity
{
    public int ProductId { get; set; }
    public int ComponentMaterialId { get; set; }
    public decimal QuantityRequired { get; set; }
    public string? Notes { get; set; }

    public Material? Product { get; set; }
    public Material? ComponentMaterial { get; set; }
}

public class MaterialMovement : Entity
{
    public int MaterialId { get; set; }
    public string MovementType { get; set; } = string.Empty; // Receipt, Issue, Transfer, Adjustment
    public decimal Quantity { get; set; }
    public string? FromLocation { get; set; }
    public string? ToLocation { get; set; }
    public int? WorkOrderId { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? PerformedBy { get; set; }
    public DateTime MovementDate { get; set; } = DateTime.UtcNow;

    public Material? Material { get; set; }
    public WorkOrder? WorkOrder { get; set; }
}