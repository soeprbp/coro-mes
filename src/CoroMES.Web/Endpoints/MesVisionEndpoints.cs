using CoroMES.Core.Entities;
using CoroMES.Infrastructure.Data;
using CoroMES.Web.Services;
using Microsoft.EntityFrameworkCore;

namespace CoroMES.Web.Endpoints;

public static class MesVisionEndpoints
{
    public static RouteGroupBuilder MapMesVisionEndpoints(this RouteGroupBuilder api)
    {
        var group = api.MapGroup("/integration/mes-vision")
            .WithTags("MES-Vision");

        group.MapPost("/collect", async (IMesVisionCollectorRunner collector, CancellationToken cancellationToken) =>
        {
            var result = await collector.CollectOnceAsync(cancellationToken);
            return Results.Ok(result);
        })
        .WithName("CollectMesVisionSnapshot");

        group.MapGet("/sources", async (ApplicationDbContext db, CancellationToken cancellationToken) =>
        {
            var sources = await db.VisionSources
                .AsNoTracking()
                .OrderBy(item => item.DisplayName)
                .Select(item => new
                {
                    item.Id,
                    item.ExternalSystemId,
                    item.DisplayName,
                    item.EndpointBaseUrl,
                    item.DashboardBaseUrl,
                    item.EquipmentId,
                    item.IsActive,
                    item.LastSeenAtUtc,
                    item.LastCollectedAtUtc,
                    item.LastError,
                    CameraCount = item.Cameras.Count,
                    ZoneCount = item.Zones.Count
                })
                .ToListAsync(cancellationToken);

            return Results.Ok(sources);
        })
        .WithName("GetMesVisionSources");

        group.MapGet("/mappings", async (ApplicationDbContext db, CancellationToken cancellationToken) =>
        {
            var equipment = await db.Equipment
                .AsNoTracking()
                .Where(item => item.IsActive)
                .OrderBy(item => item.Code)
                .Select(item => new MesVisionEquipmentOptionDto(item.Id, item.Code, item.Name, item.Location))
                .ToListAsync(cancellationToken);

            var cameras = await db.VisionCameras
                .AsNoTracking()
                .Include(item => item.VisionSource)
                .Include(item => item.Equipment)
                .OrderBy(item => item.VisionSource!.DisplayName)
                .ThenBy(item => item.DisplayName)
                .Select(item => new MesVisionCameraMappingDto(
                    item.Id,
                    item.VisionSourceId,
                    item.VisionSource!.DisplayName,
                    item.ElementId,
                    item.DisplayName,
                    item.SlotId,
                    item.Source,
                    item.SourceType,
                    item.EquipmentId,
                    item.Equipment == null ? null : item.Equipment.Code,
                    item.Equipment == null ? null : item.Equipment.Name,
                    item.IsActive,
                    item.LastSeenAtUtc,
                    item.LastStatus,
                    item.LastMotionLevel,
                    item.Zones.Count))
                .ToListAsync(cancellationToken);

            var zones = await db.VisionZones
                .AsNoTracking()
                .Include(item => item.VisionSource)
                .Include(item => item.VisionCamera)
                .Include(item => item.Equipment)
                .OrderBy(item => item.VisionSource!.DisplayName)
                .ThenBy(item => item.VisionCamera == null ? string.Empty : item.VisionCamera.DisplayName)
                .ThenBy(item => item.Name)
                .Select(item => new MesVisionZoneMappingDto(
                    item.Id,
                    item.VisionSourceId,
                    item.VisionSource!.DisplayName,
                    item.VisionCameraId,
                    item.VisionCamera == null ? null : item.VisionCamera.DisplayName,
                    item.ElementId,
                    item.Name,
                    item.EquipmentId,
                    item.Equipment == null ? null : item.Equipment.Code,
                    item.Equipment == null ? null : item.Equipment.Name,
                    item.Enabled,
                    item.LastSeenAtUtc,
                    item.LastStatus,
                    item.LastMotionPercent))
                .ToListAsync(cancellationToken);

            return Results.Ok(new MesVisionMappingSnapshotDto(equipment, cameras, zones));
        })
        .WithName("GetMesVisionMappings");

        group.MapPut("/cameras/{id:int}/equipment", async (
            int id,
            MesVisionEquipmentMappingRequest request,
            ApplicationDbContext db,
            IAuditLogService auditLog,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            var camera = await db.VisionCameras
                .Include(item => item.VisionSource)
                .Include(item => item.Equipment)
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (camera is null)
            {
                await auditLog.RecordAsync(context, new AuditLogEntry(
                    "MapEquipment",
                    nameof(VisionCamera),
                    id,
                    $"MES-Vision camera mapping failed because id {id} was not found.",
                    Succeeded: false));
                return Results.NotFound();
            }

            if (!await EquipmentExistsAsync(db, request.EquipmentId, cancellationToken))
            {
                await auditLog.RecordAsync(context, new AuditLogEntry(
                    "MapEquipment",
                    nameof(VisionCamera),
                    id,
                    $"MES-Vision camera {camera.ElementId} mapping rejected because equipment id {request.EquipmentId} was not found.",
                    Succeeded: false));
                return Results.BadRequest(new { message = $"Equipment id {request.EquipmentId} was not found." });
            }

            var before = DescribeEquipment(camera.EquipmentId, camera.Equipment?.Code);
            camera.EquipmentId = request.EquipmentId;
            camera.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);

            await auditLog.RecordAsync(context, new AuditLogEntry(
                "MapEquipment",
                nameof(VisionCamera),
                camera.Id,
                $"Camera={camera.DisplayName}; Source={camera.VisionSource?.DisplayName ?? camera.VisionSourceId.ToString()}; {before} -> {DescribeEquipment(request.EquipmentId, null)}"));

            return Results.Ok(new MesVisionMappingUpdateDto(camera.Id, request.EquipmentId));
        })
        .WithName("MapMesVisionCameraEquipment");

        group.MapPut("/zones/{id:int}/equipment", async (
            int id,
            MesVisionEquipmentMappingRequest request,
            ApplicationDbContext db,
            IAuditLogService auditLog,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            var zone = await db.VisionZones
                .Include(item => item.VisionSource)
                .Include(item => item.VisionCamera)
                .Include(item => item.Equipment)
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (zone is null)
            {
                await auditLog.RecordAsync(context, new AuditLogEntry(
                    "MapEquipment",
                    nameof(VisionZone),
                    id,
                    $"MES-Vision zone mapping failed because id {id} was not found.",
                    Succeeded: false));
                return Results.NotFound();
            }

            if (!await EquipmentExistsAsync(db, request.EquipmentId, cancellationToken))
            {
                await auditLog.RecordAsync(context, new AuditLogEntry(
                    "MapEquipment",
                    nameof(VisionZone),
                    id,
                    $"MES-Vision zone {zone.ElementId} mapping rejected because equipment id {request.EquipmentId} was not found.",
                    Succeeded: false));
                return Results.BadRequest(new { message = $"Equipment id {request.EquipmentId} was not found." });
            }

            var before = DescribeEquipment(zone.EquipmentId, zone.Equipment?.Code);
            zone.EquipmentId = request.EquipmentId;
            zone.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);

            await auditLog.RecordAsync(context, new AuditLogEntry(
                "MapEquipment",
                nameof(VisionZone),
                zone.Id,
                $"Zone={zone.Name}; Camera={zone.VisionCamera?.DisplayName ?? "none"}; Source={zone.VisionSource?.DisplayName ?? zone.VisionSourceId.ToString()}; {before} -> {DescribeEquipment(request.EquipmentId, null)}"));

            return Results.Ok(new MesVisionMappingUpdateDto(zone.Id, request.EquipmentId));
        })
        .WithName("MapMesVisionZoneEquipment");

        group.MapGet("/readings", async (int? take, ApplicationDbContext db, CancellationToken cancellationToken) =>
        {
            var limit = Math.Clamp(take ?? 100, 1, 1000);
            var readings = await db.VisionReadings
                .AsNoTracking()
                .OrderByDescending(item => item.OccurredAtUtc)
                .Take(limit)
                .Select(item => new
                {
                    item.Id,
                    item.VisionSourceId,
                    item.VisionCameraId,
                    item.VisionZoneId,
                    item.ElementId,
                    item.Metric,
                    item.OccurredAtUtc,
                    item.CollectedAtUtc,
                    item.NumericValue,
                    item.TextValue,
                    item.BooleanValue,
                    item.Quality
                })
                .ToListAsync(cancellationToken);

            return Results.Ok(readings);
        })
        .WithName("GetMesVisionReadings");

        group.MapGet("/events", async (int? take, ApplicationDbContext db, CancellationToken cancellationToken) =>
        {
            var limit = Math.Clamp(take ?? 100, 1, 1000);
            var events = await db.VisionEvents
                .AsNoTracking()
                .OrderByDescending(item => item.OccurredAtUtc)
                .Take(limit)
                .Select(item => new
                {
                    item.Id,
                    item.VisionSourceId,
                    item.VisionCameraId,
                    item.VisionZoneId,
                    item.OccurredAtUtc,
                    item.CollectedAtUtc,
                    item.Source,
                    item.Status,
                    item.MotionPercent,
                    item.Region
                })
                .ToListAsync(cancellationToken);

            return Results.Ok(events);
        })
        .WithName("GetMesVisionEvents");

        return api;
    }

    private static async Task<bool> EquipmentExistsAsync(ApplicationDbContext db, int? equipmentId, CancellationToken cancellationToken)
    {
        return !equipmentId.HasValue ||
            await db.Equipment.AnyAsync(item => item.Id == equipmentId.Value, cancellationToken);
    }

    private static string DescribeEquipment(int? equipmentId, string? equipmentCode)
    {
        return equipmentId.HasValue
            ? $"EquipmentId={equipmentId.Value}{(string.IsNullOrWhiteSpace(equipmentCode) ? string.Empty : $"; EquipmentCode={equipmentCode}")}"
            : "EquipmentId=none";
    }
}

public sealed record MesVisionMappingSnapshotDto(
    IReadOnlyList<MesVisionEquipmentOptionDto> Equipment,
    IReadOnlyList<MesVisionCameraMappingDto> Cameras,
    IReadOnlyList<MesVisionZoneMappingDto> Zones);

public sealed record MesVisionEquipmentOptionDto(int Id, string Code, string Name, string? Location);

public sealed record MesVisionCameraMappingDto(
    int Id,
    int VisionSourceId,
    string VisionSourceName,
    string ElementId,
    string DisplayName,
    string? SlotId,
    string? Source,
    string? SourceType,
    int? EquipmentId,
    string? EquipmentCode,
    string? EquipmentName,
    bool IsActive,
    DateTime? LastSeenAtUtc,
    string? LastStatus,
    double? LastMotionLevel,
    int ZoneCount);

public sealed record MesVisionZoneMappingDto(
    int Id,
    int VisionSourceId,
    string VisionSourceName,
    int? VisionCameraId,
    string? VisionCameraName,
    string ElementId,
    string Name,
    int? EquipmentId,
    string? EquipmentCode,
    string? EquipmentName,
    bool Enabled,
    DateTime? LastSeenAtUtc,
    string? LastStatus,
    double? LastMotionPercent);

public sealed record MesVisionEquipmentMappingRequest(int? EquipmentId);

public sealed record MesVisionMappingUpdateDto(int Id, int? EquipmentId);
