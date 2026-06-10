using CoroMES.Integration.Upkeep;
using CoroMES.Infrastructure.Data;
using CoroMES.Web.Services;
using Microsoft.EntityFrameworkCore;

namespace CoroMES.Web.Endpoints;

public static class UpkeepEndpoints
{
    public static RouteGroupBuilder MapUpkeepEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/integration/upkeep/assets", async (IUpkeepIntegration upkeep, CancellationToken cancellationToken) =>
        {
            var assets = await upkeep.GetAssetsAsync(cancellationToken);
            return Results.Ok(new
            {
                mode = upkeep.Mode,
                assets
            });
        })
        .WithName("GetUpkeepAssets")
        .WithTags("Upkeep");

        api.MapPost("/integration/upkeep/sync", async (ApplicationDbContext db, IUpkeepIntegration upkeep, IAuditLogService auditLog, HttpContext context, CancellationToken cancellationToken) =>
        {
            var equipmentWithUpkeep = await db.Equipment
                .Where(e => e.UpkeepAssetId != null)
                .ToListAsync(cancellationToken);

            var request = new UpkeepSyncRequest(equipmentWithUpkeep
                .Select(item => new UpkeepEquipmentLink(
                    item.Id,
                    item.Code,
                    item.Name,
                    item.UpkeepAssetId!.Value,
                    item.Status.ToString(),
                    item.Location))
                .ToList());

            var result = await upkeep.SyncEquipmentAsync(request, cancellationToken);

            await auditLog.RecordAsync(context, new AuditLogEntry(
                "Sync",
                "Upkeep",
                null,
                $"{result.Message} Mode={result.Mode}; Synced={result.Synced}",
                Succeeded: result.Success));

            return result.Success ? Results.Ok(result) : Results.BadRequest(result);
        })
        .WithName("SyncUpkeep")
        .WithTags("Upkeep");

        api.MapPost("/integration/upkeep/downtime", async (UpkeepDowntimeRequest? request, IUpkeepIntegration upkeep, IAuditLogService auditLog, HttpContext context, CancellationToken cancellationToken) =>
        {
            var result = await upkeep.LogDowntimeAsync(
                request ?? new UpkeepDowntimeRequest(null, null, null, DateTime.UtcNow),
                cancellationToken);

            await auditLog.RecordAsync(context, new AuditLogEntry(
                "Downtime",
                "Upkeep",
                null,
                $"{result.Message} Mode={result.Mode}",
                Succeeded: result.Success));

            return result.Success ? Results.Ok(result) : Results.BadRequest(result);
        })
        .WithName("LogDowntime")
        .WithTags("Upkeep");

        return api;
    }
}
