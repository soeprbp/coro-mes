using CoroMES.Core.Entities;
using CoroMES.Core.Interfaces.Repositories;
using CoroMES.Industrial.i3X;
using CoroMES.Infrastructure.Data;
using CoroMES.Infrastructure.Repositories;
using CoroMES.Infrastructure.Repositories.i3x;
using CoroMES.Web.Components;
using CoroMES.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Encodings.Web;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/access-denied";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Events.OnRedirectToLogin = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            }

            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            }

            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});

builder.Services.AddSingleton<IUpkeepAssetCatalog, UpkeepAssetCatalog>();

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

var useI3X = builder.Configuration.GetValue<bool?>("i3x:enabled") ?? false;

if (useI3X)
{
    builder.Services.AddI3X(builder.Configuration);
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

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

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

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.Use(async (context, next) =>
{
    if (RequiresAdminGate(context.Request.Path) && context.User.Identity?.IsAuthenticated != true)
    {
        await context.ChallengeAsync();
        return;
    }

    await next();
});

app.MapGet("/login", (HttpContext context, IWebHostEnvironment environment, IConfiguration configuration) =>
{
    var returnUrl = GetSafeReturnUrl(context.Request.Query["returnUrl"].ToString());
    var error = context.Request.Query.ContainsKey("error");
    var devHint = environment.IsDevelopment() && string.IsNullOrWhiteSpace(configuration["Auth:AdminAccessCode"])
        ? "<p class=\"hint\">Development access code: <code>dev-admin</code></p>"
        : string.Empty;
    var errorHtml = error ? "<p class=\"error\">Access code was not accepted.</p>" : string.Empty;
    var encodedReturnUrl = HtmlEncoder.Default.Encode(returnUrl);

    return Results.Content($$"""
        <!doctype html>
        <html lang="en">
        <head>
            <meta charset="utf-8" />
            <meta name="viewport" content="width=device-width, initial-scale=1" />
            <title>CoroMES Admin Sign In</title>
            <style>
                body { margin: 0; min-height: 100vh; display: grid; place-items: center; background: #f5f7fa; color: #17202a; font-family: Segoe UI, Arial, sans-serif; }
                main { width: min(420px, calc(100vw - 32px)); padding: 28px; border: 1px solid #d9e0e8; border-radius: 8px; background: #fff; }
                h1 { margin: 0 0 8px; font-size: 1.45rem; }
                p { color: #607080; }
                label { display: grid; gap: 8px; margin-top: 18px; color: #607080; font-size: .9rem; }
                input { padding: 10px 12px; border: 1px solid #d9e0e8; border-radius: 6px; font: inherit; }
                button { width: 100%; margin-top: 18px; padding: 10px 12px; border: 1px solid #176b87; border-radius: 6px; background: #176b87; color: #fff; font: inherit; cursor: pointer; }
                .error { color: #b42318; font-weight: 700; }
                .hint code { color: #17202a; }
            </style>
        </head>
        <body>
            <main>
                <h1>CoroMES Admin</h1>
                <p>Sign in to manage equipment, integrations, and MES records.</p>
                {{errorHtml}}
                {{devHint}}
                <form method="post" action="/login">
                    <input type="hidden" name="returnUrl" value="{{encodedReturnUrl}}" />
                    <label>Access code <input name="accessCode" type="password" autocomplete="current-password" required autofocus /></label>
                    <button type="submit">Sign In</button>
                </form>
            </main>
        </body>
        </html>
        """, "text/html");
}).AllowAnonymous();

app.MapPost("/login", async (HttpContext context, IWebHostEnvironment environment, IConfiguration configuration) =>
{
    var form = await context.Request.ReadFormAsync();
    var returnUrl = GetSafeReturnUrl(form["returnUrl"].ToString());
    var accessCode = form["accessCode"].ToString();
    var configuredAccessCode = configuration["Auth:AdminAccessCode"];
    var expectedAccessCode = !string.IsNullOrWhiteSpace(configuredAccessCode)
        ? configuredAccessCode
        : environment.IsDevelopment() ? "dev-admin" : null;

    if (string.IsNullOrWhiteSpace(expectedAccessCode) || !TimeSafeEquals(accessCode, expectedAccessCode))
    {
        return Results.Redirect($"/login?error=1&returnUrl={Uri.EscapeDataString(returnUrl)}");
    }

    var claims = new[]
    {
        new Claim(ClaimTypes.Name, "CoroMES Admin"),
        new Claim(ClaimTypes.Role, "Admin")
    };
    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    var principal = new ClaimsPrincipal(identity);

    await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
    return Results.Redirect(returnUrl);
}).AllowAnonymous();

app.MapGet("/logout", async (HttpContext context) =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/login");
});

app.MapGet("/access-denied", () => Results.Content("""
    <!doctype html>
    <html lang="en">
    <head><meta charset="utf-8" /><meta name="viewport" content="width=device-width, initial-scale=1" /><title>Access Denied</title></head>
    <body style="font-family: Segoe UI, Arial, sans-serif; margin: 40px;">
        <h1>Access denied</h1>
        <p>Your account does not have permission to view this CoroMES area.</p>
        <a href="/login">Sign in again</a>
    </body>
    </html>
    """, "text/html")).AllowAnonymous();

app.MapGet("/admin/index.html", () => Results.Redirect("/admin", permanent: false));
app.MapGet("/displays/builder.html", () => Results.Redirect("/displays/builder", permanent: false));
app.MapGet("/displays/viewer.html", (HttpContext context) =>
{
    var query = context.Request.QueryString.HasValue ? context.Request.QueryString.Value : string.Empty;
    return Results.Redirect($"/displays/viewer{query}", permanent: false);
});

app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
    .WithName("Health Check");

MapApiRoutes(app);

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

static string GetSafeReturnUrl(string? returnUrl)
{
    if (string.IsNullOrWhiteSpace(returnUrl) || !returnUrl.StartsWith("/", StringComparison.Ordinal) || returnUrl.StartsWith("//", StringComparison.Ordinal))
    {
        return "/admin";
    }

    return returnUrl;
}

static bool TimeSafeEquals(string candidate, string expected)
{
    var candidateBytes = System.Text.Encoding.UTF8.GetBytes(candidate);
    var expectedBytes = System.Text.Encoding.UTF8.GetBytes(expected);
    if (candidateBytes.Length != expectedBytes.Length)
    {
        return false;
    }

    return System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(candidateBytes, expectedBytes);
}

static bool RequiresAdminGate(PathString path)
{
    return path.StartsWithSegments("/admin") ||
        string.Equals(path.Value, "/displays", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWithSegments("/displays/builder");
}

static void MapApiRoutes(WebApplication app)
{
    var api = app.MapGroup("/api/v1").RequireAuthorization("AdminOnly");

    api.MapGet("/workorders", async (IWorkOrderRepository workOrderRepo) =>
    {
        var workOrders = await workOrderRepo.GetAllAsync();
        return Results.Ok(workOrders.Take(100));
    })
    .WithName("GetWorkOrders")
    .WithTags("WorkOrders");

    api.MapGet("/workorders/{id}", async (int id, IWorkOrderRepository workOrderRepo) =>
    {
        var workOrder = await workOrderRepo.GetByIdAsync(id);
        return workOrder is null ? Results.NotFound() : Results.Ok(workOrder);
    })
    .WithName("GetWorkOrder")
    .WithTags("WorkOrders");

    api.MapPost("/workorders", async (WorkOrder workOrder, IWorkOrderRepository workOrderRepo) =>
    {
        workOrder.CreatedAt = DateTime.UtcNow;
        workOrder.Number = $"WO-{DateTime.UtcNow:yyyyMMddHHmmss}";
        await workOrderRepo.AddAsync(workOrder);
        return Results.Created($"/api/v1/workorders/{workOrder.Id}", workOrder);
    })
    .WithName("CreateWorkOrder")
    .WithTags("WorkOrders");

    api.MapGet("/equipment", async (IEquipmentRepository equipmentRepo) =>
    {
        var equipment = await equipmentRepo.GetAllAsync();
        return Results.Ok(equipment.Take(100));
    })
    .WithName("GetEquipment")
    .WithTags("Equipment");

    api.MapGet("/equipment/{id}", async (int id, IEquipmentRepository equipmentRepo) =>
    {
        var equipment = await equipmentRepo.GetByIdAsync(id);
        return equipment is null ? Results.NotFound() : Results.Ok(equipment);
    })
    .WithName("GetEquipmentById")
    .WithTags("Equipment");

    api.MapPost("/equipment", async (Equipment equipment, IEquipmentRepository equipmentRepo) =>
    {
        equipment.CreatedAt = DateTime.UtcNow;
        await equipmentRepo.AddAsync(equipment);
        return Results.Created($"/api/v1/equipment/{equipment.Id}", equipment);
    })
    .WithName("CreateEquipment")
    .WithTags("Equipment");

    api.MapPut("/equipment/{id}", async (int id, Equipment equipment, IEquipmentRepository equipmentRepo) =>
    {
        var existing = await equipmentRepo.GetByIdAsync(id);
        if (existing is null)
        {
            return Results.NotFound();
        }

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

    api.MapDelete("/equipment/{id}", async (int id, IEquipmentRepository equipmentRepo) =>
    {
        var equipment = await equipmentRepo.GetByIdAsync(id);
        if (equipment is null)
        {
            return Results.NotFound();
        }

        await equipmentRepo.DeleteAsync(id);
        return Results.NoContent();
    })
    .WithName("DeleteEquipment")
    .WithTags("Equipment");

    api.MapGet("/integration/upkeep/assets", (IUpkeepAssetCatalog assets) => Results.Ok(assets.GetAssets()))
    .WithName("GetUpkeepAssets")
    .WithTags("Upkeep");

    api.MapPost("/integration/upkeep/sync", async (ApplicationDbContext db) =>
    {
        var equipmentWithUpkeep = await db.Equipment
            .Where(e => e.UpkeepAssetId != null)
            .ToListAsync();

        return Results.Ok(new
        {
            synced = equipmentWithUpkeep.Count,
            timestamp = DateTime.UtcNow,
            message = "Equipment status synced to Upkeep"
        });
    })
    .WithName("SyncUpkeep")
    .WithTags("Upkeep");

    api.MapPost("/integration/upkeep/downtime", () =>
    {
        return Results.Ok(new { success = true, message = "Downtime logged to Upkeep" });
    })
    .WithName("LogDowntime")
    .WithTags("Upkeep");

    api.MapGet("/materials", async (IMaterialRepository materialRepo) =>
    {
        var materials = await materialRepo.GetAllAsync();
        return Results.Ok(materials.Take(100));
    })
    .WithName("GetMaterials")
    .WithTags("Materials");

    api.MapGet("/materials/{id}", async (int id, IMaterialRepository materialRepo) =>
    {
        var material = await materialRepo.GetByIdAsync(id);
        return material is null ? Results.NotFound() : Results.Ok(material);
    })
    .WithName("GetMaterial")
    .WithTags("Materials");

    api.MapPost("/materials", async (Material material, IMaterialRepository materialRepo) =>
    {
        material.CreatedAt = DateTime.UtcNow;
        await materialRepo.AddAsync(material);
        return Results.Created($"/api/v1/materials/{material.Id}", material);
    })
    .WithName("CreateMaterial")
    .WithTags("Materials");

    api.MapGet("/operators", async (IOperatorRepository operatorRepo) =>
    {
        var operators = await operatorRepo.GetAllAsync();
        return Results.Ok(operators.Take(100));
    })
    .WithName("GetOperators")
    .WithTags("Operators");

    api.MapGet("/operators/{id}", async (int id, IOperatorRepository operatorRepo) =>
    {
        var op = await operatorRepo.GetByIdAsync(id);
        return op is null ? Results.NotFound() : Results.Ok(op);
    })
    .WithName("GetOperator")
    .WithTags("Operators");

    api.MapPost("/operators", async (Operator op, IOperatorRepository operatorRepo) =>
    {
        op.CreatedAt = DateTime.UtcNow;
        await operatorRepo.AddAsync(op);
        return Results.Created($"/api/v1/operators/{op.Id}", op);
    })
    .WithName("CreateOperator")
    .WithTags("Operators");

    api.MapGet("/quality/inspections", async (IInspectionRepository inspectionRepo) =>
    {
        var inspections = await inspectionRepo.GetAllAsync();
        return Results.Ok(inspections.Take(100));
    })
    .WithName("GetInspections")
    .WithTags("Quality");

    api.MapGet("/quality/ncr", async (INonConformanceRepository ncrRepo) =>
    {
        var ncrs = await ncrRepo.GetAllAsync();
        return Results.Ok(ncrs.Take(100));
    })
    .WithName("GetNonConformances")
    .WithTags("Quality");

    api.MapGet("/displays", () =>
    {
        return Results.Ok(new[]
        {
            new { id = "line1-oee", name = "Line 1 OEE", type = "oee", refreshSeconds = 5 },
            new { id = "quality", name = "Quality Monitor", type = "quality", refreshSeconds = 10 },
            new { id = "equipment", name = "Equipment Status", type = "equipment", refreshSeconds = 3 },
            new { id = "production", name = "Production Floor", type = "production", refreshSeconds = 5 }
        });
    })
    .WithName("GetDisplays")
    .WithTags("Displays");

    api.MapGet("/displays/{id}", (string id) =>
    {
        return Results.Ok(new { id, name = "Display", type = "oee", refreshSeconds = 5, equipment = Array.Empty<int>() });
    })
    .WithName("GetDisplay")
    .WithTags("Displays");
}

public partial class Program { }
