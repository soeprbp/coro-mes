using CoroMES.Core.Interfaces.Repositories;
using CoroMES.Industrial.i3X;
using CoroMES.Integration.Alerts;
using CoroMES.Integration.IIoT;
using CoroMES.Integration.Upkeep;
using CoroMES.Infrastructure.Data;
using CoroMES.Infrastructure.Repositories;
using CoroMES.Infrastructure.Repositories.i3x;
using CoroMES.Web.Components;
using CoroMES.Web.Endpoints;
using CoroMES.Web.Services;
using CoroMES.Web.Startup;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

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

builder.Services.AddAlertingIntegration(builder.Configuration);
builder.Services.AddMesVisionCollector(builder.Configuration);
builder.Services.AddUpkeepIntegration(builder.Configuration);
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<IAlarmService, AlarmService>();
builder.Services.AddScoped<IMesVisionCollectorRunner, MesVisionCollectorRunner>();
builder.Services.AddScoped<ISystemSettingsService, SystemSettingsService>();
builder.Services.AddHostedService<MesVisionCollectorHostedService>();

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

await app.EnsureCoroMesDatabaseAsync(dbProvider);

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.Use(async (context, next) =>
{
    if (AuthEndpoints.RequiresAdminGate(context.Request.Path) && context.User.Identity?.IsAuthenticated != true)
    {
        await context.ChallengeAsync();
        return;
    }

    await next();
});

app.MapAuthEndpoints();
app.MapCompatibilityEndpoints();
app.MapCoroMesApiEndpoints();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

public partial class Program { }
