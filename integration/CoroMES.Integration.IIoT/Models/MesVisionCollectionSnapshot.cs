namespace CoroMES.Integration.IIoT.Models;

public sealed record MesVisionCollectionSnapshot(
    MesVisionSourceSnapshot Source,
    IReadOnlyList<MesVisionCameraSnapshot> Cameras,
    IReadOnlyList<MesVisionZoneSnapshot> Zones,
    IReadOnlyList<MesVisionReadingSnapshot> Readings,
    IReadOnlyList<MesVisionEventSnapshot> Events,
    DateTime CollectedAtUtc);

public sealed record MesVisionSourceSnapshot(
    string ExternalSystemId,
    string DisplayName,
    string EndpointBaseUrl,
    string? DashboardBaseUrl,
    DateTime LastSeenAtUtc,
    string? ServerVersion,
    string? SpecVersion);

public sealed record MesVisionCameraSnapshot(
    string ElementId,
    string DisplayName,
    string? SlotId,
    string? Source,
    string? SourceType,
    string? Status,
    double? MotionLevel,
    DateTime LastSeenAtUtc);

public sealed record MesVisionZoneSnapshot(
    string ElementId,
    string? CameraElementId,
    string Name,
    bool Enabled,
    string? Status,
    double? MotionPercent,
    DateTime LastSeenAtUtc);

public sealed record MesVisionReadingSnapshot(
    string ElementId,
    string? CameraElementId,
    string? ZoneElementId,
    string Metric,
    DateTime OccurredAtUtc,
    DateTime CollectedAtUtc,
    double? NumericValue,
    string? TextValue,
    bool? BooleanValue,
    string? Quality,
    string RawJson);

public sealed record MesVisionEventSnapshot(
    string ElementId,
    string? CameraElementId,
    string? ZoneElementId,
    DateTime OccurredAtUtc,
    DateTime CollectedAtUtc,
    string? Source,
    string Status,
    double? MotionPercent,
    string? Region,
    string RawJson);
