using System.Text.Json;
using System.Text.Json.Serialization;

namespace CoroMES.Industrial.i3X.Models;

/// <summary>
/// Namespace definition
/// </summary>
public class Namespace
{
    [JsonPropertyName("uri")]
    public string Uri { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;
}

/// <summary>
/// Object Type definition (schema for objects)
/// </summary>
public class ObjectType
{
    public string ElementId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string NamespaceUri { get; set; } = string.Empty;

    [JsonIgnore]
    public string? SourceTypeId { get; set; }

    [JsonIgnore]
    public string? Version { get; set; }

    public object Schema { get; set; } = new(); // JSON Schema object
}

/// <summary>
/// Relationship Type definition
/// </summary>
public class RelationshipType
{
    public string ElementId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string NamespaceUri { get; set; } = string.Empty;

    [JsonIgnore]
    public string RelationshipId { get; set; } = string.Empty;

    public string ReverseOf { get; set; } = string.Empty;
}

/// <summary>
/// Object instance metadata
/// </summary>
public class ObjectMetadata
{
    public string? Description { get; set; }
    public string? TypeNamespaceUri { get; set; }
    public string? SourceTypeId { get; set; }
    public Dictionary<string, List<string>>? Relationships { get; set; }
    public Dictionary<string, object>? ExtendedAttributes { get; set; }
    public Dictionary<string, object>? System { get; set; }
}

/// <summary>
/// Object instance (equipment, sensor, etc.)
/// </summary>
public class ObjectInstance
{
    public string ElementId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;

    [JsonIgnore]
    public string TypeId { get; set; } = string.Empty;

    [JsonIgnore]
    public string TypeElementId
    {
        get => TypeId;
        set => TypeId = value;
    }

    [JsonPropertyName("typeElementId")]
    public string? TypeElementIdJson
    {
        get => string.IsNullOrWhiteSpace(TypeId) ? null : TypeId;
        set
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                TypeId = value;
            }
        }
    }

    [JsonPropertyName("typeId")]
    public string? LegacyTypeIdJson
    {
        get => null;
        set
        {
            if (!string.IsNullOrWhiteSpace(value) && string.IsNullOrWhiteSpace(TypeId))
            {
                TypeId = value;
            }
        }
    }

    public string? ParentId { get; set; }
    public bool IsComposition { get; set; }

    public string NamespaceUri { get; set; } = string.Empty;

    public Dictionary<string, object>? Relationships { get; set; }

    [JsonIgnore]
    public bool IsExtended { get; set; }
}

/// <summary>
/// Object instance with metadata
/// </summary>
public class ObjectWithMetadata : ObjectInstance
{
    public ObjectMetadata? Metadata { get; set; }
}

/// <summary>
/// Value read response for an object
/// </summary>
public class ValueReadResult
{
    public bool IsComposition { get; set; }
    public List<Vqt<object>> Data { get; set; } = new();
    public List<Vqt<object>> Values { get; set; } = new();
    public string? Quality { get; set; }
    public DateTime? Timestamp { get; set; }

    [JsonPropertyName("value")]
    public object? CurrentValue { get; set; }

    [JsonIgnore]
    public Vqt<object> Value
    {
        get
        {
            if (Data.FirstOrDefault() is { } dataValue)
            {
                return dataValue;
            }

            if (CurrentValue != null || Quality != null || Timestamp.HasValue)
            {
                return new Vqt<object>
                {
                    Value = CurrentValue,
                    Quality = Quality,
                    Timestamp = Timestamp
                };
            }

            return Values.FirstOrDefault() ?? new Vqt<object>();
        }
    }

    public Dictionary<string, ValueReadResult>? Components { get; set; }
}

/// <summary>
/// Bulk query request for object values
/// </summary>
public class ObjectsValueRequest
{
    public List<string> ElementIds { get; set; } = new();
    public int? MaxDepth { get; set; }
}

public class ObjectsWriteRequest
{
    public List<ObjectValueUpdate> Updates { get; set; } = new();
}

public class ObjectValueUpdate
{
    public string ElementId { get; set; } = string.Empty;
    public Vqt<object> Value { get; set; } = new();
}

public class CreateSubscriptionRequest
{
    public string ClientId { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
}

public class SubscriptionIdsRequest
{
    public string ClientId { get; set; } = string.Empty;
    public List<string> SubscriptionIds { get; set; } = new();
}

public class SubscriptionItemsRequest
{
    public string ClientId { get; set; } = string.Empty;
    public string SubscriptionId { get; set; } = string.Empty;
    public List<string> ElementIds { get; set; } = new();
    public int? MaxDepth { get; set; }
}

public class SubscriptionSyncRequest
{
    public string ClientId { get; set; } = string.Empty;
    public string SubscriptionId { get; set; } = string.Empty;
    public long? LastSequenceNumber { get; set; }
}

public class CreateSubscriptionResponse
{
    public string SubscriptionId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class SubscriptionsResponse
{
    public List<SubscriptionSummary> SubscriptionIds { get; set; } = new();
}

public class SubscriptionSummary
{
    public JsonElement SubscriptionId { get; set; }
    public string Created { get; set; } = string.Empty;
}

/// <summary>
/// API error response
/// </summary>
public class ApiError
{
    public int Code { get; set; }
    public string Message { get; set; } = string.Empty;
}
