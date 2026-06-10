using CoroMES.Core.Entities;
using CoroMES.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CoroMES.Web.Services;

public interface IAuditLogService
{
    Task RecordAsync(HttpContext? context, AuditLogEntry entry);
    Task<IReadOnlyList<AuditLog>> GetRecentAsync(string? entityName = null, int take = 100);
}

public sealed class AuditLogService : IAuditLogService
{
    private readonly ApplicationDbContext _db;

    public AuditLogService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task RecordAsync(HttpContext? context, AuditLogEntry entry)
    {
        var audit = new AuditLog
        {
            OccurredAtUtc = DateTime.UtcNow,
            Actor = entry.Actor ?? GetActor(context),
            Action = entry.Action,
            EntityName = entry.EntityName,
            EntityId = entry.EntityId,
            Route = entry.Route ?? context?.Request.Path.Value,
            IpAddress = context?.Connection.RemoteIpAddress?.ToString(),
            UserAgent = context?.Request.Headers.UserAgent.ToString(),
            Succeeded = entry.Succeeded,
            Summary = entry.Summary
        };

        await _db.AuditLogs.AddAsync(audit);
        await _db.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<AuditLog>> GetRecentAsync(string? entityName = null, int take = 100)
    {
        var boundedTake = Math.Clamp(take, 1, 500);
        var query = _db.AuditLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(entityName))
        {
            query = query.Where(log => log.EntityName == entityName);
        }

        return await query
            .OrderByDescending(log => log.OccurredAtUtc)
            .Take(boundedTake)
            .ToListAsync();
    }

    private static string GetActor(HttpContext? context)
    {
        return context?.User.FindFirstValue(ClaimTypes.Name) ??
            context?.User.Identity?.Name ??
            "unknown";
    }
}

public sealed record AuditLogEntry(
    string Action,
    string EntityName,
    int? EntityId,
    string Summary,
    bool Succeeded = true,
    string? Actor = null,
    string? Route = null);
