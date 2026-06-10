using CoroMES.Core.Entities;
using CoroMES.Core.Enums;
using CoroMES.Infrastructure.Data;
using CoroMES.Integration.Alerts;
using Microsoft.EntityFrameworkCore;

namespace CoroMES.Web.Services;

public interface IAlarmService
{
    Task<IReadOnlyList<AlarmEvent>> GetRecentAsync(AlarmStatus? status = null, int take = 100, CancellationToken cancellationToken = default);
    Task<AlarmEvent?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<AlarmEvent> RaiseAsync(AlarmCreateRequest request, string actor, CancellationToken cancellationToken = default);
    Task<AlarmEvent?> AcknowledgeAsync(int id, string actor, CancellationToken cancellationToken = default);
    Task<AlarmEvent?> ResolveAsync(int id, string actor, CancellationToken cancellationToken = default);
}

public sealed class AlarmService(ApplicationDbContext db, IAlertDispatcher alertDispatcher) : IAlarmService
{
    public async Task<IReadOnlyList<AlarmEvent>> GetRecentAsync(AlarmStatus? status = null, int take = 100, CancellationToken cancellationToken = default)
    {
        var query = db.AlarmEvents
            .AsNoTracking()
            .Include(alarm => alarm.Equipment)
            .OrderByDescending(alarm => alarm.TriggeredAtUtc)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(alarm => alarm.Status == status.Value);
        }

        return await query.Take(Math.Clamp(take, 1, 500)).ToListAsync(cancellationToken);
    }

    public Task<AlarmEvent?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return db.AlarmEvents
            .AsNoTracking()
            .Include(alarm => alarm.Equipment)
            .FirstOrDefaultAsync(alarm => alarm.Id == id, cancellationToken);
    }

    public async Task<AlarmEvent> RaiseAsync(AlarmCreateRequest request, string actor, CancellationToken cancellationToken = default)
    {
        var severity = request.Severity ?? AlarmSeverity.Warning;
        var channels = request.Channels is { Count: > 0 }
            ? NormalizeChannels(request.Channels)
            : alertDispatcher.GetDefaultChannels(severity.ToString()).ToList();

        var alarm = new AlarmEvent
        {
            Source = Clean(request.Source, "manual", 80),
            Title = Clean(request.Title, "Alarm", 160),
            Message = Clean(request.Message, "Alarm raised.", 1000),
            Severity = severity,
            Status = AlarmStatus.Active,
            EquipmentId = request.EquipmentId,
            TriggeredAtUtc = DateTime.UtcNow,
            AlertChannels = string.Join(",", channels),
            CreatedBy = actor
        };

        var dispatch = await alertDispatcher.DispatchAsync(new AlertMessage(
            alarm.Title,
            alarm.Message,
            alarm.Severity.ToString(),
            alarm.Source,
            alarm.EquipmentId,
            channels), cancellationToken);

        alarm.NotificationSummary = dispatch.Summary;
        await db.AlarmEvents.AddAsync(alarm, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return alarm;
    }

    public Task<AlarmEvent?> AcknowledgeAsync(int id, string actor, CancellationToken cancellationToken = default)
    {
        return TransitionAsync(id, actor, AlarmStatus.Acknowledged, cancellationToken);
    }

    public Task<AlarmEvent?> ResolveAsync(int id, string actor, CancellationToken cancellationToken = default)
    {
        return TransitionAsync(id, actor, AlarmStatus.Resolved, cancellationToken);
    }

    private async Task<AlarmEvent?> TransitionAsync(int id, string actor, AlarmStatus status, CancellationToken cancellationToken)
    {
        var alarm = await db.AlarmEvents.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (alarm is null)
        {
            return null;
        }

        alarm.Status = status;
        alarm.UpdatedAt = DateTime.UtcNow;
        alarm.UpdatedBy = actor;

        if (status == AlarmStatus.Acknowledged)
        {
            alarm.AcknowledgedAtUtc ??= DateTime.UtcNow;
            alarm.AcknowledgedBy ??= actor;
        }
        else if (status == AlarmStatus.Resolved)
        {
            alarm.ResolvedAtUtc ??= DateTime.UtcNow;
            alarm.ResolvedBy ??= actor;
            alarm.AcknowledgedAtUtc ??= DateTime.UtcNow;
            alarm.AcknowledgedBy ??= actor;
        }

        await db.SaveChangesAsync(cancellationToken);
        return alarm;
    }

    private static List<string> NormalizeChannels(IEnumerable<string> channels)
    {
        return channels
            .Select(channel => channel.Trim().ToLowerInvariant())
            .Where(channel => !string.IsNullOrWhiteSpace(channel))
            .Distinct()
            .ToList();
    }

    private static string Clean(string? value, string fallback, int maxLength)
    {
        var cleaned = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        return cleaned.Length <= maxLength ? cleaned : cleaned[..maxLength];
    }
}

public sealed record AlarmCreateRequest(
    string? Title,
    string? Message,
    AlarmSeverity? Severity,
    string? Source,
    int? EquipmentId,
    IReadOnlyList<string>? Channels);
