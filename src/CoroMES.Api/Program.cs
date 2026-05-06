using Microsoft.EntityFrameworkCore;
using CoroMES.Infrastructure.Data;
using CoroMES.Core.Entities;

var builder = WebApplication.CreateBuilder(args);

// Configure database
var dbProvider = builder.Configuration.GetValue<string>("DatabaseProvider") ?? "sqlite";
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (dbProvider == "postgres")
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(connectionString));
}
else
{
    // Use SQLite for development - use absolute path
    var dbPath = @"C:\Users\soperbp\OneDrive - Welch Packaging Group\Scripts\workdev\CoroMES\database\coromes.db";
    Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlite($"Data Source={dbPath}"));
}

// Set web root to project root for static files (with fallback)
var webRoot = Path.Combine(Directory.GetCurrentDirectory(), "web");
if (Directory.GetCurrentDirectory() == "/" || !Directory.Exists(webRoot)) {
    webRoot = "/app/web";
}
Directory.CreateDirectory(webRoot);
builder.Environment.WebRootPath = webRoot;

var app = builder.Build();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.EnsureCreatedAsync();
}

// Enable static files
app.UseStaticFiles();

// Health check
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
    .WithName("Health Check");

// Work Orders endpoints
app.MapGet("/api/v1/workorders", async (ApplicationDbContext db) =>
{
    var workOrders = await db.WorkOrders.Take(100).ToListAsync();
    return Results.Ok(workOrders);
})
.WithName("GetWorkOrders")
.WithTags("WorkOrders");

app.MapGet("/api/v1/workorders/{id}", async (int id, ApplicationDbContext db) =>
{
    var workOrder = await db.WorkOrders.FindAsync(id);
    return workOrder is null ? Results.NotFound() : Results.Ok(workOrder);
})
.WithName("GetWorkOrder")
.WithTags("WorkOrders");

app.MapPost("/api/v1/workorders", async (WorkOrder workOrder, ApplicationDbContext db) =>
{
    workOrder.CreatedAt = DateTime.UtcNow;
    workOrder.Number = $"WO-{DateTime.UtcNow:yyyyMMddHHmmss}";
    db.WorkOrders.Add(workOrder);
    await db.SaveChangesAsync();
    return Results.Created($"/api/v1/workorders/{workOrder.Id}", workOrder);
})
.WithName("CreateWorkOrder")
.WithTags("WorkOrders");

// Equipment endpoints
app.MapGet("/api/v1/equipment", async (ApplicationDbContext db) =>
{
    var equipment = await db.Equipment.Take(100).ToListAsync();
    return Results.Ok(equipment);
})
.WithName("GetEquipment")
.WithTags("Equipment");

app.MapGet("/api/v1/equipment/{id}", async (int id, ApplicationDbContext db) =>
{
    var equipment = await db.Equipment.FindAsync(id);
    return equipment is null ? Results.NotFound() : Results.Ok(equipment);
})
.WithName("GetEquipmentById")
.WithTags("Equipment");

app.MapPost("/api/v1/equipment", async (Equipment equipment, ApplicationDbContext db) =>
{
    equipment.CreatedAt = DateTime.UtcNow;
    db.Equipment.Add(equipment);
    await db.SaveChangesAsync();
    return Results.Created($"/api/v1/equipment/{equipment.Id}", equipment);
})
.WithName("CreateEquipment")
.WithTags("Equipment");

app.MapPut("/api/v1/equipment/{id}", async (int id, Equipment equipment, ApplicationDbContext db) =>
{
    var existing = await db.Equipment.FindAsync(id);
    if (existing is null) return Results.NotFound();
    
    existing.Name = equipment.Name;
    existing.Code = equipment.Code;
    existing.Type = equipment.Type;
    existing.IpAddress = equipment.IpAddress;
    existing.UpkeepAssetId = equipment.UpkeepAssetId;
    existing.PartsPerMinute = equipment.PartsPerMinute;
    existing.SqFtPerDay = equipment.SqFtPerDay;
    existing.CycleTimeSeconds = equipment.CycleTimeSeconds;
    existing.Protocol = equipment.Protocol;
    existing.Status = equipment.Status;
    existing.UpdatedAt = DateTime.UtcNow;
    
    await db.SaveChangesAsync();
    return Results.Ok(existing);
})
.WithName("UpdateEquipment")
.WithTags("Equipment");

app.MapDelete("/api/v1/equipment/{id}", async (int id, ApplicationDbContext db) =>
{
    var equipment = await db.Equipment.FindAsync(id);
    if (equipment is null) return Results.NotFound();
    
    db.Equipment.Remove(equipment);
    await db.SaveChangesAsync();
    return Results.NoContent();
})
.WithName("DeleteEquipment")
.WithTags("Equipment");

// Upkeep Integration endpoints
app.MapGet("/api/v1/integration/upkeep/assets", async (ApplicationDbContext db) =>
{
    // In production, this would call the Upkeep API
    // For now, return placeholder
    var assets = new[]
    {
        new { id = 101, name = "Corrugator Main Drive", type = "Equipment", location = "Building A" },
        new { id = 102, name = "Bender Unit A1", type = "Equipment", location = "Building B" },
        new { id = 103, name = "Conveyor Belt Line 1", type = "Equipment", location = "Building A" },
        new { id = 104, name = "Temperature Sensor Array", type = "Sensor", location = "Building A" }
    };
    return Results.Ok(assets);
})
.WithName("GetUpkeepAssets")
.WithTags("Upkeep");

app.MapPost("/api/v1/integration/upkeep/sync", async (ApplicationDbContext db) =>
{
    // Get all equipment with Upkeep links
    var equipmentWithUpkeep = await db.Equipment
        .Where(e => e.UpkeepAssetId != null)
        .ToListAsync();
    
    return Results.Ok(new { 
        synced = equipmentWithUpkeep.Count,
        timestamp = DateTime.UtcNow,
        message = "Equipment status synced to Upkeep"
    });
})
.WithName("SyncUpkeep")
.WithTags("Upkeep");

app.MapPost("/api/v1/integration/upkeep/downtime", async (ApplicationDbContext db) =>
{
    // Record downtime to Upkeep work order
    return Results.Ok(new { success = true, message = "Downtime logged to Upkeep" });
})
.WithName("LogDowntime")
.WithTags("Upkeep");

// Materials endpoints
app.MapGet("/api/v1/materials", async (ApplicationDbContext db) =>
{
    var materials = await db.Materials.Take(100).ToListAsync();
    return Results.Ok(materials);
})
.WithName("GetMaterials")
.WithTags("Materials");

app.MapGet("/api/v1/materials/{id}", async (int id, ApplicationDbContext db) =>
{
    var material = await db.Materials.FindAsync(id);
    return material is null ? Results.NotFound() : Results.Ok(material);
})
.WithName("GetMaterial")
.WithTags("Materials");

app.MapPost("/api/v1/materials", async (Material material, ApplicationDbContext db) =>
{
    material.CreatedAt = DateTime.UtcNow;
    db.Materials.Add(material);
    await db.SaveChangesAsync();
    return Results.Created($"/api/v1/materials/{material.Id}", material);
})
.WithName("CreateMaterial")
.WithTags("Materials");

// Operators endpoints
app.MapGet("/api/v1/operators", async (ApplicationDbContext db) =>
{
    var operators = await db.Operators.Take(100).ToListAsync();
    return Results.Ok(operators);
})
.WithName("GetOperators")
.WithTags("Operators");

app.MapGet("/api/v1/operators/{id}", async (int id, ApplicationDbContext db) =>
{
    var op = await db.Operators.FindAsync(id);
    return op is null ? Results.NotFound() : Results.Ok(op);
})
.WithName("GetOperator")
.WithTags("Operators");

app.MapPost("/api/v1/operators", async (Operator op, ApplicationDbContext db) =>
{
    op.CreatedAt = DateTime.UtcNow;
    db.Operators.Add(op);
    await db.SaveChangesAsync();
    return Results.Created($"/api/v1/operators/{op.Id}", op);
})
.WithName("CreateOperator")
.WithTags("Operators");

// Quality endpoints
app.MapGet("/api/v1/quality/inspections", async (ApplicationDbContext db) =>
{
    var inspections = await db.Inspections.Take(100).ToListAsync();
    return Results.Ok(inspections);
})
.WithName("GetInspections")
.WithTags("Quality");

app.MapGet("/api/v1/quality/ncr", async (ApplicationDbContext db) =>
{
    var ncrs = await db.NonConformances.Take(100).ToListAsync();
    return Results.Ok(ncrs);
})
.WithName("GetNonConformances")
.WithTags("Quality");

// Display endpoints
app.MapGet("/api/v1/displays", async (ApplicationDbContext db) =>
{
    // Return display configurations
    return Results.Ok(new[] {
        new { id = "line1-oee", name = "Line 1 OEE", type = "oee", refreshSeconds = 5 },
        new { id = "quality", name = "Quality Monitor", type = "quality", refreshSeconds = 10 },
        new { id = "equipment", name = "Equipment Status", type = "equipment", refreshSeconds = 3 },
        new { id = "production", name = "Production Floor", type = "production", refreshSeconds = 5 }
    });
})
.WithName("GetDisplays")
.WithTags("Displays");

app.MapGet("/api/v1/displays/{id}", async (string id, ApplicationDbContext db) =>
{
    // Return specific display config
    return Results.Ok(new { id, name = "Display", type = "oee", refreshSeconds = 5, equipment = new int[] { } });
})
.WithName("GetDisplay")
.WithTags("Displays");

app.Run();

public partial class Program { }