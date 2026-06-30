using CoroMES.Core.Entities;
using CoroMES.Infrastructure.Data;
using CoroMES.Integration.IIoT.Abstractions;
using CoroMES.Integration.IIoT.Models;
using Microsoft.EntityFrameworkCore;

namespace CoroMES.Web.Services;

public interface IMesVisionCollectorRunner
{
    Task<MesVisionCollectionPersistResult> CollectOnceAsync(CancellationToken cancellationToken = default);
}

public sealed record MesVisionCollectionPersistResult(
    int VisionSourceId,
    int CamerasUpserted,
    int ZonesUpserted,
    int ReadingsInserted,
    int EventsInserted,
    DateTime CollectedAtUtc);

public sealed class MesVisionCollectorRunner(
    IMesVisionCollectorClient collectorClient,
    ApplicationDbContext db,
    ILogger<MesVisionCollectorRunner> logger) : IMesVisionCollectorRunner
{
    public async Task<MesVisionCollectionPersistResult> CollectOnceAsync(CancellationToken cancellationToken = default)
    {
        var snapshot = await collectorClient.CollectAsync(cancellationToken);
        var source = await UpsertSourceAsync(snapshot.Source, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        var cameras = await UpsertCamerasAsync(source.Id, snapshot.Cameras, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        var zones = await UpsertZonesAsync(source.Id, snapshot.Zones, cameras, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        var insertedReadings = await InsertReadingsAsync(source.Id, snapshot.Readings, cameras, zones, cancellationToken);
        var insertedEvents = await InsertEventsAsync(source.Id, snapshot.Events, cameras, zones, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Persisted MES-Vision snapshot: source={VisionSourceId}, cameras={CameraCount}, zones={ZoneCount}, readings={ReadingCount}, events={EventCount}",
            source.Id,
            cameras.Count,
            zones.Count,
            insertedReadings,
            insertedEvents);

        return new MesVisionCollectionPersistResult(
            source.Id,
            cameras.Count,
            zones.Count,
            insertedReadings,
            insertedEvents,
            snapshot.CollectedAtUtc);
    }

    private async Task<VisionSource> UpsertSourceAsync(MesVisionSourceSnapshot snapshot, CancellationToken cancellationToken)
    {
        var source = await db.VisionSources
            .FirstOrDefaultAsync(item => item.ExternalSystemId == snapshot.ExternalSystemId, cancellationToken);

        if (source is null)
        {
            source = new VisionSource
            {
                ExternalSystemId = snapshot.ExternalSystemId,
                CreatedAt = DateTime.UtcNow
            };
            db.VisionSources.Add(source);
        }

        source.DisplayName = TruncateRequired(snapshot.DisplayName, 160);
        source.EndpointBaseUrl = TruncateRequired(snapshot.EndpointBaseUrl, 500);
        source.DashboardBaseUrl = Truncate(snapshot.DashboardBaseUrl, 500);
        source.IsActive = true;
        source.LastSeenAtUtc = snapshot.LastSeenAtUtc;
        source.LastCollectedAtUtc = snapshot.LastSeenAtUtc;
        source.LastError = null;
        source.UpdatedAt = DateTime.UtcNow;

        return source;
    }

    private async Task<Dictionary<string, VisionCamera>> UpsertCamerasAsync(
        int sourceId,
        IReadOnlyList<MesVisionCameraSnapshot> snapshots,
        CancellationToken cancellationToken)
    {
        var existing = await db.VisionCameras
            .Where(item => item.VisionSourceId == sourceId)
            .ToDictionaryAsync(item => item.ElementId, StringComparer.OrdinalIgnoreCase, cancellationToken);

        foreach (var snapshot in snapshots)
        {
            if (!existing.TryGetValue(snapshot.ElementId, out var camera))
            {
                camera = new VisionCamera
                {
                    VisionSourceId = sourceId,
                    ElementId = snapshot.ElementId,
                    CreatedAt = DateTime.UtcNow
                };
                db.VisionCameras.Add(camera);
                existing[snapshot.ElementId] = camera;
            }

            camera.DisplayName = TruncateRequired(snapshot.DisplayName, 160);
            camera.SlotId = Truncate(snapshot.SlotId, 80);
            camera.Source = Truncate(snapshot.Source, 200);
            camera.SourceType = Truncate(snapshot.SourceType, 80);
            camera.LastStatus = Truncate(snapshot.Status, 80);
            camera.LastMotionLevel = snapshot.MotionLevel;
            camera.LastSeenAtUtc = snapshot.LastSeenAtUtc;
            camera.IsActive = true;
            camera.UpdatedAt = DateTime.UtcNow;
        }

        return existing;
    }

    private async Task<Dictionary<string, VisionZone>> UpsertZonesAsync(
        int sourceId,
        IReadOnlyList<MesVisionZoneSnapshot> snapshots,
        IReadOnlyDictionary<string, VisionCamera> cameras,
        CancellationToken cancellationToken)
    {
        var existing = await db.VisionZones
            .Where(item => item.VisionSourceId == sourceId)
            .ToDictionaryAsync(item => item.ElementId, StringComparer.OrdinalIgnoreCase, cancellationToken);

        foreach (var snapshot in snapshots)
        {
            if (!existing.TryGetValue(snapshot.ElementId, out var zone))
            {
                zone = new VisionZone
                {
                    VisionSourceId = sourceId,
                    ElementId = snapshot.ElementId,
                    CreatedAt = DateTime.UtcNow
                };
                db.VisionZones.Add(zone);
                existing[snapshot.ElementId] = zone;
            }

            zone.VisionCameraId = snapshot.CameraElementId is not null && cameras.TryGetValue(snapshot.CameraElementId, out var camera)
                ? camera.Id
                : null;
            zone.Name = TruncateRequired(snapshot.Name, 160);
            zone.Enabled = snapshot.Enabled;
            zone.LastStatus = Truncate(snapshot.Status, 80);
            zone.LastMotionPercent = snapshot.MotionPercent;
            zone.LastSeenAtUtc = snapshot.LastSeenAtUtc;
            zone.UpdatedAt = DateTime.UtcNow;
        }

        return existing;
    }

    private async Task<int> InsertReadingsAsync(
        int sourceId,
        IReadOnlyList<MesVisionReadingSnapshot> snapshots,
        IReadOnlyDictionary<string, VisionCamera> cameras,
        IReadOnlyDictionary<string, VisionZone> zones,
        CancellationToken cancellationToken)
    {
        var inserted = 0;

        foreach (var snapshot in snapshots)
        {
            var exists = await db.VisionReadings.AnyAsync(item =>
                item.VisionSourceId == sourceId &&
                item.ElementId == snapshot.ElementId &&
                item.Metric == snapshot.Metric &&
                item.OccurredAtUtc == snapshot.OccurredAtUtc,
                cancellationToken);

            if (exists)
            {
                continue;
            }

            VisionZone? zone = null;
            if (snapshot.ZoneElementId is not null)
            {
                zones.TryGetValue(snapshot.ZoneElementId, out zone);
            }
            else
            {
                zones.TryGetValue(snapshot.ElementId, out zone);
            }

            VisionCamera? camera = null;
            if (snapshot.CameraElementId is not null)
            {
                cameras.TryGetValue(snapshot.CameraElementId, out camera);
            }
            else
            {
                cameras.TryGetValue(snapshot.ElementId, out camera);
            }

            db.VisionReadings.Add(new VisionReading
            {
                VisionSourceId = sourceId,
                VisionCameraId = camera?.Id ?? zone?.VisionCameraId,
                VisionZoneId = zone?.Id,
                ElementId = TruncateRequired(snapshot.ElementId, 160),
                Metric = TruncateRequired(snapshot.Metric, 120),
                OccurredAtUtc = snapshot.OccurredAtUtc,
                CollectedAtUtc = snapshot.CollectedAtUtc,
                NumericValue = snapshot.NumericValue,
                TextValue = Truncate(snapshot.TextValue, 500),
                BooleanValue = snapshot.BooleanValue,
                Quality = Truncate(snapshot.Quality, 80),
                RawJson = Truncate(snapshot.RawJson, 4000) ?? "null"
            });
            inserted++;
        }

        return inserted;
    }

    private async Task<int> InsertEventsAsync(
        int sourceId,
        IReadOnlyList<MesVisionEventSnapshot> snapshots,
        IReadOnlyDictionary<string, VisionCamera> cameras,
        IReadOnlyDictionary<string, VisionZone> zones,
        CancellationToken cancellationToken)
    {
        var inserted = 0;

        foreach (var snapshot in snapshots)
        {
            var exists = await db.VisionEvents.AnyAsync(item =>
                item.VisionSourceId == sourceId &&
                item.OccurredAtUtc == snapshot.OccurredAtUtc &&
                item.Status == snapshot.Status &&
                item.MotionPercent == snapshot.MotionPercent &&
                item.Region == snapshot.Region,
                cancellationToken);

            if (exists)
            {
                continue;
            }

            VisionZone? zone = null;
            if (snapshot.ZoneElementId is not null)
            {
                zones.TryGetValue(snapshot.ZoneElementId, out zone);
            }
            else
            {
                zones.TryGetValue(snapshot.ElementId, out zone);
            }

            VisionCamera? camera = null;
            if (snapshot.CameraElementId is not null)
            {
                cameras.TryGetValue(snapshot.CameraElementId, out camera);
            }
            else
            {
                cameras.TryGetValue(snapshot.ElementId, out camera);
            }

            db.VisionEvents.Add(new VisionEvent
            {
                VisionSourceId = sourceId,
                VisionCameraId = camera?.Id ?? zone?.VisionCameraId,
                VisionZoneId = zone?.Id,
                OccurredAtUtc = snapshot.OccurredAtUtc,
                CollectedAtUtc = snapshot.CollectedAtUtc,
                Source = Truncate(snapshot.Source, 200),
                Status = Truncate(snapshot.Status, 80) ?? "unknown",
                MotionPercent = snapshot.MotionPercent,
                Region = Truncate(snapshot.Region, 160),
                RawJson = Truncate(snapshot.RawJson, 4000) ?? "{}"
            });
            inserted++;
        }

        return inserted;
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
        {
            return value;
        }

        return value[..maxLength];
    }

    private static string TruncateRequired(string value, int maxLength)
    {
        return Truncate(value, maxLength) ?? string.Empty;
    }
}
