using CoroMES.Core.Entities;
using CoroMES.Core.Enums;
using CoroMES.Web.Services;

namespace CoroMES.Web.Endpoints;

public static class AlarmEndpoints
{
    public static RouteGroupBuilder MapAlarmEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/alarms", async (AlarmStatus? status, int? take, IAlarmService alarms) =>
        {
            var results = await alarms.GetRecentAsync(status, take ?? 100);
            return Results.Ok(results.Select(ToResponse));
        })
        .WithName("GetAlarms")
        .WithTags("Alarms");

        api.MapGet("/alarms/{id:int}", async (int id, IAlarmService alarms) =>
        {
            var alarm = await alarms.GetByIdAsync(id);
            return alarm is null ? Results.NotFound() : Results.Ok(ToResponse(alarm));
        })
        .WithName("GetAlarm")
        .WithTags("Alarms");

        api.MapPost("/alarms", async (AlarmCreateRequest request, IAlarmService alarms, IAuditLogService auditLog, HttpContext context) =>
        {
            var actor = context.User.Identity?.Name ?? "unknown";
            var alarm = await alarms.RaiseAsync(request, actor, context.RequestAborted);

            await auditLog.RecordAsync(context, new AuditLogEntry(
                "Create",
                nameof(AlarmEvent),
                alarm.Id,
                $"Severity={alarm.Severity}; Source={alarm.Source}; Channels={alarm.AlertChannels ?? "none"}"));

            return Results.Created($"/api/v1/alarms/{alarm.Id}", ToResponse(alarm));
        })
        .WithName("CreateAlarm")
        .WithTags("Alarms");

        api.MapPost("/alarms/{id:int}/acknowledge", async (int id, IAlarmService alarms, IAuditLogService auditLog, HttpContext context) =>
        {
            var actor = context.User.Identity?.Name ?? "unknown";
            var alarm = await alarms.AcknowledgeAsync(id, actor, context.RequestAborted);
            if (alarm is null)
            {
                return Results.NotFound();
            }

            await auditLog.RecordAsync(context, new AuditLogEntry(
                "Acknowledge",
                nameof(AlarmEvent),
                alarm.Id,
                $"Alarm acknowledged by {actor}."));

            return Results.Ok(ToResponse(alarm));
        })
        .WithName("AcknowledgeAlarm")
        .WithTags("Alarms");

        api.MapPost("/alarms/{id:int}/resolve", async (int id, IAlarmService alarms, IAuditLogService auditLog, HttpContext context) =>
        {
            var actor = context.User.Identity?.Name ?? "unknown";
            var alarm = await alarms.ResolveAsync(id, actor, context.RequestAborted);
            if (alarm is null)
            {
                return Results.NotFound();
            }

            await auditLog.RecordAsync(context, new AuditLogEntry(
                "Resolve",
                nameof(AlarmEvent),
                alarm.Id,
                $"Alarm resolved by {actor}."));

            return Results.Ok(ToResponse(alarm));
        })
        .WithName("ResolveAlarm")
        .WithTags("Alarms");

        return api;
    }

    private static AlarmResponse ToResponse(AlarmEvent alarm)
    {
        return new AlarmResponse(
            alarm.Id,
            alarm.Title,
            alarm.Message,
            alarm.Severity.ToString(),
            alarm.Status.ToString(),
            alarm.Source,
            alarm.EquipmentId,
            alarm.Equipment?.Code,
            alarm.Equipment?.Name,
            alarm.TriggeredAtUtc,
            alarm.AlertChannels,
            alarm.NotificationSummary);
    }

    private sealed record AlarmResponse(
        int Id,
        string Title,
        string Message,
        string Severity,
        string Status,
        string Source,
        int? EquipmentId,
        string? EquipmentCode,
        string? EquipmentName,
        DateTime TriggeredAtUtc,
        string? AlertChannels,
        string? NotificationSummary);
}
