using CoroMES.Core.Entities;
using CoroMES.Web.Services;

namespace CoroMES.Web.Endpoints;

public static class SettingsEndpoints
{
    public static RouteGroupBuilder MapSettingsEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/settings", async (ISystemSettingsService settings, HttpContext context, CancellationToken cancellationToken) =>
        {
            var actor = context.User.Identity?.Name ?? "unknown";
            return Results.Ok(await settings.GetSnapshotAsync(actor, cancellationToken));
        })
        .WithName("GetSystemSettings")
        .WithTags("Settings");

        api.MapPut("/settings", async (
            SystemSettingsUpdateRequest request,
            ISystemSettingsService settings,
            IAuditLogService auditLog,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            var actor = context.User.Identity?.Name ?? "unknown";
            SystemSettingsSnapshot snapshot;

            try
            {
                snapshot = await settings.SaveAsync(request, actor, cancellationToken);
            }
            catch (InvalidOperationException ex)
            {
                await auditLog.RecordAsync(context, new AuditLogEntry(
                    "UpdateRejected",
                    nameof(SystemSetting),
                    null,
                    $"Rejected non-secret settings update: {ex.Message}",
                    Succeeded: false));

                return Results.BadRequest(new { error = ex.Message });
            }

            await auditLog.RecordAsync(context, new AuditLogEntry(
                "Update",
                nameof(SystemSetting),
                null,
                "Updated non-secret system settings."));

            return Results.Ok(snapshot);
        })
        .WithName("UpdateSystemSettings")
        .WithTags("Settings");

        return api;
    }
}
