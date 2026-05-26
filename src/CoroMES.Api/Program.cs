using Microsoft.EntityFrameworkCore;
using CoroMES.Infrastructure.Data;
using CoroMES.Core.Entities;
using CoroMES.Core.Interfaces.Repositories;
using CoroMES.Infrastructure.Repositories;
using CoroMES.Infrastructure.Repositories.i3x;
using CoroMES.Industrial.i3X;

var builder = WebApplication.CreateBuilder(args);

// Configure database
var dbProvider = builder.Configuration.GetValue<string>("DatabaseProvider") ?? "sqlite";
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.Equals(dbProvider, "postgres", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(connectionString));
}
else
{
    var sqliteConnectionString = string.IsNullOrWhiteSpace(connectionString)
        ? "Data Source=database/coromes.db"
        : connectionString;

    var dbPath = sqliteConnectionString
        .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .FirstOrDefault(part => part.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase))
        ?.Split('=', 2)[1];

    if (!string.IsNullOrWhiteSpace(dbPath))
    {
        var resolvedDbPath = Path.IsPathRooted(dbPath)
            ? dbPath
            : Path.Combine(builder.Environment.ContentRootPath, dbPath);

        Directory.CreateDirectory(Path.GetDirectoryName(resolvedDbPath)!);
        sqliteConnectionString = $"Data Source={resolvedDbPath}";
    }

    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlite(sqliteConnectionString));
}

// Determine if i3X is enabled
var useI3X = builder.Configuration.GetValue<bool?>("i3x:enabled") ?? false;

if (useI3X)
{
    // Register i3X services
    builder.Services.AddI3X(builder.Configuration);

    // Register i3X-backed repositories for all repository interfaces.
    builder.Services.AddScoped<IWorkOrderRepository, I3XWorkOrderRepository>();
    builder.Services.AddScoped<IWorkOrderOperationRepository, I3XWorkOrderOperationRepository>();
    builder.Services.AddScoped<IEquipmentRepository, I3XEquipmentRepository>();
    builder.Services.AddScoped<IEquipmentMaintenanceRepository, I3XEquipmentMaintenanceRepository>();
    builder.Services.AddScoped<IMaterialRepository, I3XMaterialRepository>();
    builder.Services.AddScoped<IBillOfMaterialsRepository, I3XBillOfMaterialsRepository>();
    builder.Services.AddScoped<IMaterialMovementRepository, I3XMaterialMovementRepository>();
    builder.Services.AddScoped<IOperatorRepository, I3XOperatorRepository>();
    builder.Services.AddScoped<IShiftRepository, I3XShiftRepository>();
    builder.Services.AddScoped<ILaborRecordRepository, I3XLaborRecordRepository>();
    builder.Services.AddScoped<IInspectionRepository, I3XInspectionRepository>();
    builder.Services.AddScoped<IInspectionItemRepository, I3XInspectionItemRepository>();
    builder.Services.AddScoped<INonConformanceRepository, I3XNonConformanceRepository>();
}
else
{
    // Register EF Core repositories (existing)
    builder.Services.AddScoped<IWorkOrderRepository, WorkOrderRepository>();
    builder.Services.AddScoped<IWorkOrderOperationRepository, WorkOrderOperationRepository>();
    builder.Services.AddScoped<IEquipmentRepository, EquipmentRepository>();
    builder.Services.AddScoped<IEquipmentMaintenanceRepository, EquipmentMaintenanceRepository>();
    builder.Services.AddScoped<IMaterialRepository, MaterialRepository>();
    builder.Services.AddScoped<IBillOfMaterialsRepository, BillOfMaterialsRepository>();
    builder.Services.AddScoped<IMaterialMovementRepository, MaterialMovementRepository>();
    builder.Services.AddScoped<IOperatorRepository, OperatorRepository>();
    builder.Services.AddScoped<IShiftRepository, ShiftRepository>();
    builder.Services.AddScoped<ILaborRecordRepository, LaborRecordRepository>();
    builder.Services.AddScoped<IInspectionRepository, InspectionRepository>();
    builder.Services.AddScoped<IInspectionItemRepository, InspectionItemRepository>();
    builder.Services.AddScoped<INonConformanceRepository, NonConformanceRepository>();
}

// Set web root to project root for static files (with fallback)
var webRoot = Path.Combine(Directory.GetCurrentDirectory(), "web");
if (Directory.GetCurrentDirectory() == "/" || !Directory.Exists(webRoot)) {
    webRoot = "/app/web";
}
Directory.CreateDirectory(webRoot);
builder.Environment.WebRootPath = webRoot;

var app = builder.Build();

// Apply migrations for PostgreSQL. SQLite is the local Windows test store and
// uses EnsureCreated because the checked-in migrations target PostgreSQL.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    if (string.Equals(dbProvider, "postgres", StringComparison.OrdinalIgnoreCase))
    {
        await db.Database.MigrateAsync();
    }
    else
    {
        await db.Database.EnsureCreatedAsync();
    }
}

// Enable static files
app.UseStaticFiles();

// Health check
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
    .WithName("Health Check");

// Work Orders endpoints
app.MapGet("/api/v1/workorders", async (IWorkOrderRepository workOrderRepo) =>
{
    var workOrders = await workOrderRepo.GetAllAsync();
    return Results.Ok(workOrders.Take(100));
})
.WithName("GetWorkOrders")
.WithTags("WorkOrders");

app.MapGet("/api/v1/workorders/{id}", async (int id, IWorkOrderRepository workOrderRepo) =>
{
    var workOrder = await workOrderRepo.GetByIdAsync(id);
    return workOrder is null ? Results.NotFound() : Results.Ok(workOrder);
})
.WithName("GetWorkOrder")
.WithTags("WorkOrders");

app.MapPost("/api/v1/workorders", async (WorkOrder workOrder, IWorkOrderRepository workOrderRepo) =>
{
    workOrder.CreatedAt = DateTime.UtcNow;
    workOrder.Number = $"WO-{DateTime.UtcNow:yyyyMMddHHmmss}";
    await workOrderRepo.AddAsync(workOrder);
    return Results.Created($"/api/v1/workorders/{workOrder.Id}", workOrder);
})
.WithName("CreateWorkOrder")
.WithTags("WorkOrders");

// Equipment endpoints
app.MapGet("/api/v1/equipment", async (IEquipmentRepository equipmentRepo) =>
{
    var equipment = await equipmentRepo.GetAllAsync();
    return Results.Ok(equipment.Take(100));
})
.WithName("GetEquipment")
.WithTags("Equipment");

app.MapGet("/api/v1/equipment/{id}", async (int id, IEquipmentRepository equipmentRepo) =>
{
    var equipment = await equipmentRepo.GetByIdAsync(id);
    return equipment is null ? Results.NotFound() : Results.Ok(equipment);
})
.WithName("GetEquipmentById")
.WithTags("Equipment");

app.MapPost("/api/v1/equipment", async (Equipment equipment, IEquipmentRepository equipmentRepo) =>
{
    equipment.CreatedAt = DateTime.UtcNow;
    await equipmentRepo.AddAsync(equipment);
    return Results.Created($"/api/v1/equipment/{equipment.Id}", equipment);
})
.WithName("CreateEquipment")
.WithTags("Equipment");

app.MapPut("/api/v1/equipment/{id}", async (int id, Equipment equipment, IEquipmentRepository equipmentRepo) =>
{
    var existing = await equipmentRepo.GetByIdAsync(id);
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
    
    await equipmentRepo.UpdateAsync(existing);
    return Results.Ok(existing);
})
.WithName("UpdateEquipment")
.WithTags("Equipment");

app.MapDelete("/api/v1/equipment/{id}", async (int id, IEquipmentRepository equipmentRepo) =>
{
    var equipment = await equipmentRepo.GetByIdAsync(id);
    if (equipment is null) return Results.NotFound();
    
    await equipmentRepo.DeleteAsync(id);
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
app.MapGet("/api/v1/materials", async (IMaterialRepository materialRepo) =>
{
    var materials = await materialRepo.GetAllAsync();
    return Results.Ok(materials.Take(100));
})
.WithName("GetMaterials")
.WithTags("Materials");

app.MapGet("/api/v1/materials/{id}", async (int id, IMaterialRepository materialRepo) =>
{
    var material = await materialRepo.GetByIdAsync(id);
    return material is null ? Results.NotFound() : Results.Ok(material);
})
.WithName("GetMaterial")
.WithTags("Materials");

app.MapPost("/api/v1/materials", async (Material material, IMaterialRepository materialRepo) =>
{
    material.CreatedAt = DateTime.UtcNow;
    await materialRepo.AddAsync(material);
    return Results.Created($"/api/v1/materials/{material.Id}", material);
})
.WithName("CreateMaterial")
.WithTags("Materials");

// Operators endpoints
app.MapGet("/api/v1/operators", async (IOperatorRepository operatorRepo) =>
{
    var operators = await operatorRepo.GetAllAsync();
    return Results.Ok(operators.Take(100));
})
.WithName("GetOperators")
.WithTags("Operators");

app.MapGet("/api/v1/operators/{id}", async (int id, IOperatorRepository operatorRepo) =>
{
    var op = await operatorRepo.GetByIdAsync(id);
    return op is null ? Results.NotFound() : Results.Ok(op);
})
.WithName("GetOperator")
.WithTags("Operators");

app.MapPost("/api/v1/operators", async (Operator op, IOperatorRepository operatorRepo) =>
{
    op.CreatedAt = DateTime.UtcNow;
    await operatorRepo.AddAsync(op);
    return Results.Created($"/api/v1/operators/{op.Id}", op);
})
.WithName("CreateOperator")
.WithTags("Operators");

// Quality endpoints
app.MapGet("/api/v1/quality/inspections", async (IInspectionRepository inspectionRepo) =>
{
    var inspections = await inspectionRepo.GetAllAsync();
    return Results.Ok(inspections.Take(100));
})
.WithName("GetInspections")
.WithTags("Quality");

app.MapGet("/api/v1/quality/ncr", async (INonConformanceRepository ncrRepo) =>
{
    var ncrs = await ncrRepo.GetAllAsync();
    return Results.Ok(ncrs.Take(100));
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
