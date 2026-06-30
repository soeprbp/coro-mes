using System.Globalization;
using System.Text.Json;
using CoroMES.Industrial.i3X.Models;
using CoroMES.Integration.IIoT.Models;

namespace CoroMES.Integration.IIoT.Services;

public sealed record MesVisionNormalizationInput(
    string ExternalSystemId,
    string DisplayName,
    string EndpointBaseUrl,
    string? DashboardBaseUrl,
    DateTime CollectedAtUtc,
    ServerInfo? ServerInfo,
    IReadOnlyList<ObjectInstance> Objects,
    IReadOnlyDictionary<string, ValueReadResult> CurrentValues,
    IReadOnlyDictionary<string, ValueReadResult> HistoryValues);

public sealed class MesVisionTelemetryNormalizer
{
    public MesVisionCollectionSnapshot Normalize(MesVisionNormalizationInput input)
    {
        var collectedAt = EnsureUtc(input.CollectedAtUtc);
        var objectsById = input.Objects.ToDictionary(item => item.ElementId, StringComparer.OrdinalIgnoreCase);
        var cameras = new List<MesVisionCameraSnapshot>();
        var zones = new List<MesVisionZoneSnapshot>();
        var readings = new List<MesVisionReadingSnapshot>();
        var events = new List<MesVisionEventSnapshot>();

        foreach (var item in input.Objects)
        {
            input.CurrentValues.TryGetValue(item.ElementId, out var value);
            var occurredAt = EnsureUtc(value?.Value.Timestamp ?? collectedAt);

            if (IsType(item, "Camera"))
            {
                cameras.Add(CreateCamera(item, value, occurredAt));
            }
            else if (IsType(item, "Zone"))
            {
                zones.Add(CreateZone(item, value, objectsById, occurredAt));
            }

            if (value is null)
            {
                continue;
            }

            var cameraElementId = ResolveCameraElementId(item, objectsById);
            var zoneElementId = IsType(item, "Zone") ? item.ElementId : null;

            if (IsEventsElement(item.ElementId))
            {
                events.AddRange(ReadEvents(item.ElementId, cameraElementId, zoneElementId, value, collectedAt));
                continue;
            }

            readings.AddRange(ReadMetrics(item.ElementId, cameraElementId, zoneElementId, value, collectedAt));
        }

        foreach (var historyEntry in input.HistoryValues)
        {
            if (IsEventsElement(historyEntry.Key))
            {
                events.AddRange(ReadEvents(historyEntry.Key, null, null, historyEntry.Value, collectedAt));
            }
        }

        var source = new MesVisionSourceSnapshot(
            input.ExternalSystemId,
            input.DisplayName,
            input.EndpointBaseUrl,
            input.DashboardBaseUrl,
            collectedAt,
            input.ServerInfo?.ServerVersion,
            input.ServerInfo?.SpecVersion);

        return new MesVisionCollectionSnapshot(
            source,
            cameras,
            zones,
            readings,
            events.DistinctBy(EventKey).OrderBy(item => item.OccurredAtUtc).ToList(),
            collectedAt);
    }

    private static MesVisionCameraSnapshot CreateCamera(ObjectInstance item, ValueReadResult? value, DateTime occurredAt)
    {
        var json = ToJsonElement(value?.Value.Value);
        return new MesVisionCameraSnapshot(
            item.ElementId,
            item.DisplayName,
            SlotFromElementId(item.ElementId),
            GetString(json, "source"),
            GetString(json, "sourceType"),
            GetString(json, "status"),
            GetDouble(json, "motionLevel"),
            occurredAt);
    }

    private static MesVisionZoneSnapshot CreateZone(
        ObjectInstance item,
        ValueReadResult? value,
        IReadOnlyDictionary<string, ObjectInstance> objectsById,
        DateTime occurredAt)
    {
        var json = ToJsonElement(value?.Value.Value);
        var cameraElementId = item.ParentId;
        if (!string.IsNullOrWhiteSpace(cameraElementId) && !objectsById.ContainsKey(cameraElementId))
        {
            cameraElementId = null;
        }

        return new MesVisionZoneSnapshot(
            item.ElementId,
            cameraElementId,
            GetString(json, "zoneName") ?? CleanZoneName(item.DisplayName),
            GetBool(json, "enabled") ?? false,
            GetString(json, "status"),
            GetDouble(json, "motionPercent"),
            occurredAt);
    }

    private static IEnumerable<MesVisionReadingSnapshot> ReadMetrics(
        string elementId,
        string? cameraElementId,
        string? zoneElementId,
        ValueReadResult value,
        DateTime collectedAt)
    {
        var occurredAt = EnsureUtc(value.Value.Timestamp ?? collectedAt);
        var rawValue = value.Value.Value;
        var json = ToJsonElement(rawValue);
        var rawJson = CompactJson(rawValue);

        if (json?.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in json.Value.EnumerateObject())
            {
                if (property.Value.ValueKind is JsonValueKind.Object or JsonValueKind.Array)
                {
                    yield return CreateReading(elementId, cameraElementId, zoneElementId, property.Name, occurredAt, collectedAt, null, property.Value.GetRawText(), null, value.Value.Quality, property.Value.GetRawText());
                    continue;
                }

                yield return CreateReading(elementId, cameraElementId, zoneElementId, property.Name, occurredAt, collectedAt, GetDouble(property.Value), GetString(property.Value), GetBool(property.Value), value.Value.Quality, property.Value.GetRawText());
            }

            yield break;
        }

        yield return CreateReading(elementId, cameraElementId, zoneElementId, "value", occurredAt, collectedAt, GetDouble(json), GetString(json), GetBool(json), value.Value.Quality, rawJson);
    }

    private static IEnumerable<MesVisionEventSnapshot> ReadEvents(
        string elementId,
        string? cameraElementId,
        string? zoneElementId,
        ValueReadResult value,
        DateTime collectedAt)
    {
        var vqts = value.Values.Count > 0 || value.Data.Count > 0
            ? value.Values.Concat(value.Data)
            : [value.Value];

        foreach (var vqt in vqts)
        {
            var json = ToJsonElement(vqt.Value);
            if (json?.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in json.Value.EnumerateArray())
                {
                    if (CreateEvent(elementId, cameraElementId, zoneElementId, item, collectedAt, vqt.Timestamp) is { } evt)
                    {
                        yield return evt;
                    }
                }

                continue;
            }

            if (json?.ValueKind == JsonValueKind.Object && CreateEvent(elementId, cameraElementId, zoneElementId, json.Value, collectedAt, vqt.Timestamp) is { } singleEvent)
            {
                yield return singleEvent;
            }
        }
    }

    private static MesVisionEventSnapshot? CreateEvent(
        string elementId,
        string? cameraElementId,
        string? zoneElementId,
        JsonElement item,
        DateTime collectedAt,
        DateTime? fallbackTimestamp)
    {
        if (item.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var occurredAt = ParseTimestamp(GetString(item, "timestamp")) ?? EnsureUtc(fallbackTimestamp ?? collectedAt);
        return new MesVisionEventSnapshot(
            elementId,
            cameraElementId,
            zoneElementId,
            occurredAt,
            EnsureUtc(collectedAt),
            GetString(item, "source"),
            GetString(item, "status") ?? "unknown",
            GetDouble(item, "motionPercent") ?? GetDouble(item, "motion_percent"),
            GetString(item, "region") ?? GetString(item, "zoneName") ?? GetString(item, "zone_name"),
            item.GetRawText());
    }

    private static MesVisionReadingSnapshot CreateReading(
        string elementId,
        string? cameraElementId,
        string? zoneElementId,
        string metric,
        DateTime occurredAt,
        DateTime collectedAt,
        double? numericValue,
        string? textValue,
        bool? booleanValue,
        string? quality,
        string rawJson)
    {
        return new MesVisionReadingSnapshot(
            elementId,
            cameraElementId,
            zoneElementId,
            metric,
            EnsureUtc(occurredAt),
            EnsureUtc(collectedAt),
            numericValue,
            textValue,
            booleanValue,
            quality,
            rawJson);
    }

    private static string EventKey(MesVisionEventSnapshot item)
    {
        return $"{item.ElementId}|{item.OccurredAtUtc:O}|{item.Status}|{item.MotionPercent?.ToString("R", CultureInfo.InvariantCulture)}|{item.Region}";
    }

    private static string? ResolveCameraElementId(ObjectInstance item, IReadOnlyDictionary<string, ObjectInstance> objectsById)
    {
        if (IsType(item, "Camera"))
        {
            return item.ElementId;
        }

        if (!string.IsNullOrWhiteSpace(item.ParentId) &&
            objectsById.TryGetValue(item.ParentId, out var parent) &&
            IsType(parent, "Camera"))
        {
            return parent.ElementId;
        }

        return null;
    }

    private static bool IsType(ObjectInstance item, string type)
    {
        return string.Equals(item.TypeElementId, type, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(item.TypeId, type, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsEventsElement(string elementId)
    {
        return string.Equals(elementId, "events", StringComparison.OrdinalIgnoreCase);
    }

    private static string? SlotFromElementId(string elementId)
    {
        return elementId.StartsWith("camera-", StringComparison.OrdinalIgnoreCase)
            ? $"cam-{elementId["camera-".Length..]}"
            : null;
    }

    private static string CleanZoneName(string displayName)
    {
        if (displayName.StartsWith("Zone ", StringComparison.OrdinalIgnoreCase))
        {
            var cleaned = displayName["Zone ".Length..];
            var index = cleaned.IndexOf(" (", StringComparison.Ordinal);
            return index >= 0 ? cleaned[..index] : cleaned;
        }

        return displayName;
    }

    private static JsonElement? ToJsonElement(object? value)
    {
        if (value is null)
        {
            return null;
        }

        if (value is JsonElement element)
        {
            return element;
        }

        return JsonSerializer.SerializeToElement(value);
    }

    private static string CompactJson(object? value)
    {
        if (value is null)
        {
            return "null";
        }

        return value is JsonElement element ? element.GetRawText() : JsonSerializer.Serialize(value);
    }

    private static string? GetString(JsonElement? element, string propertyName)
    {
        if (element is not { ValueKind: JsonValueKind.Object } value ||
            !value.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return GetString(property);
    }

    private static string? GetString(JsonElement? element)
    {
        return element?.ValueKind switch
        {
            JsonValueKind.String => element.Value.GetString(),
            JsonValueKind.Number => element.Value.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => null,
            JsonValueKind.Undefined => null,
            _ => element?.GetRawText()
        };
    }

    private static double? GetDouble(JsonElement? element, string propertyName)
    {
        if (element is not { ValueKind: JsonValueKind.Object } value ||
            !value.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return GetDouble(property);
    }

    private static double? GetDouble(JsonElement? element)
    {
        if (element?.ValueKind == JsonValueKind.Number && element.Value.TryGetDouble(out var result))
        {
            return result;
        }

        return null;
    }

    private static bool? GetBool(JsonElement? element, string propertyName)
    {
        if (element is not { ValueKind: JsonValueKind.Object } value ||
            !value.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return GetBool(property);
    }

    private static bool? GetBool(JsonElement? element)
    {
        return element?.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => null
        };
    }

    private static DateTime EnsureUtc(DateTime value)
    {
        if (value.Kind == DateTimeKind.Utc)
        {
            return value;
        }

        if (value.Kind == DateTimeKind.Local)
        {
            return value.ToUniversalTime();
        }

        return DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }

    private static DateTime? ParseTimestamp(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var offset)
            ? offset.UtcDateTime
            : null;
    }
}
