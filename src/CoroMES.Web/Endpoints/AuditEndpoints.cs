using CoroMES.Web.Services;

namespace CoroMES.Web.Endpoints;

public static class AuditEndpoints
{
    public static RouteGroupBuilder MapAuditEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/audit", async (string? entityName, int? take, IAuditLogService auditLog) =>
        {
            var logs = await auditLog.GetRecentAsync(entityName, take ?? 100);
            return Results.Ok(logs);
        })
        .WithName("GetAuditLogs")
        .WithTags("Audit");

        return api;
    }
}
