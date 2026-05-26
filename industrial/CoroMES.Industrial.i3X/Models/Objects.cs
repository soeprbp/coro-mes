namespace CoroMES.Industrial.i3X.Models;

/// <summary>
/// Namespace definition
/// </summary>
public class Namespace
{
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
    public string? SourceTypeId { get; set; }
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
    public string TypeElementId { get; set; } = string.Empty;
    public string? ParentId { get; set; }
    public bool IsComposition { get; set; }
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
/// Query result for objects (without values)
/// </summary>
public class ObjectQueryResult
{
    public string ElementId { get; set; } = string.Empty;
    public bool Success { get; set; }
    public ObjectInstance? Result { get; set; }
    public ApiError? Error { get; set; }
}

/// <summary>
/// Query result for related object lookups.
/// </summary>
public class RelatedObjectsQueryResult
{
    public string ElementId { get; set; } = string.Empty;
    public bool Success { get; set; }
    public List<ObjectInstance>? Result { get; set; }
    public ApiError? Error { get; set; }
}

/// <summary>
/// Value read response for an object
/// </summary>
public class ValueReadResult
{
    public bool IsComposition { get; set; }
    public Vqt<object> Value { get; set; } = new();
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

/// <summary>
/// Bulk query response for object values
/// </summary>
public class ObjectsValueResponseItem
{
    public string ElementId { get; set; } = string.Empty;
    public bool Success { get; set; }
    public ValueReadResult? Result { get; set; }
    public ApiError? Error { get; set; }
}

/// <summary>
/// Bulk query response for object history.
/// </summary>
public class ObjectsHistoryResponseItem
{
    public string ElementId { get; set; } = string.Empty;
    public bool Success { get; set; }
    public List<ValueReadResult>? Result { get; set; }
    public ApiError? Error { get; set; }
}

/// <summary>
/// API error response
/// </summary>
public class ApiError
{
    public int Code { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Standard i3X success/failure response wrapper
/// </summary>
public class StandardResponse<T>
{
    public bool Success { get; set; }
    public T? Result { get; set; }
    public List<T>? Results { get; set; }
    public ApiError? Error { get; set; }
}
