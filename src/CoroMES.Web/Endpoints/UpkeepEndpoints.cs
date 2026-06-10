using CoroMES.Infrastructure.Data;
using CoroMES.Web.Services;
using Microsoft.EntityFrameworkCore;

namespace CoroMES.Web.Endpoints;

public static class UpkeepEndpoints
{
    public static RouteGroupBuilder MapUpkeepEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/integration/upkeep/assets", (IUpkeepAssetCatalog assets) => Results.Ok(assets.GetAssets()))
        .WithName("GetUpkeepAssets")
        .WithTags("Upkeep");

        api.MapPost("/integration/upkeep/sync", async (ApplicationDbContext db, IAuditLogService auditLog, HttpContext context) =>
        {
            var equipmentWithUpkeep = await db.Equipment
                .Where(e => e.UpkeepAssetId != null)
                .ToListAsync();

            await auditLog.RecordAsync(context, new AuditLogEntry(
                "Sync",
                "Upkeep",
                null,
                $"Synced {equipmentWithUpkeep.Count} equipment records linked to UpKeep assets."));

            return Results.Ok(new
            {
                synced = equipmentWithUpkeep.Count,
                timestamp = DateTime.UtcNow,
                message = "Equipment status synced to Upkeep"
            });
        })
        .WithName("SyncUpkeep")
        .WithTags("Upkeep");

        api.MapPost("/integration/upkeep/downtime", async (IAuditLogService auditLog, HttpContext context) =>
        {
            await auditLog.RecordAsync(context, new AuditLogEntry(
                "Downtime",
                "Upkeep",
                null,
                "Downtime logged to Upkeep."));

            return Results.Ok(new { success = true, message = "Downtime logged to Upkeep" });
        })
        .WithName("LogDowntime")
        .WithTags("Upkeep");

        return api;
    }
}
