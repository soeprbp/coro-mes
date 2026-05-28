using System.Text.Json;

namespace CoroMES.Industrial.i3X.Models;

/// <summary>
/// Represents a Value, Quality, Timestamp (VQT) tuple used throughout i3X.
/// </summary>
public class Vqt<T>
{
    /// <summary>
    /// The actual data value. Can be null if no data available.
    /// </summary>
    public T? Value { get; set; }

    /// <summary>
    /// Data quality indicator: Good, GoodNoData, Bad, Uncertain
    /// </summary>
    public string? Quality { get; set; }

    /// <summary>
    /// RFC 3339 UTC timestamp when data was recorded.
    /// </summary>
    public DateTime? Timestamp { get; set; }
}

/// <summary>
/// Server info response from GET /info
/// </summary>
public class ServerInfo
{
    public string SpecVersion { get; set; } = string.Empty;
    public string? ServerVersion { get; set; }
    public string? ServerName { get; set; }
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public string? NamespaceUri { get; set; }
    public JsonElement? Capabilities { get; set; }
}

public class ServerCapabilities
{
    public QueryCapabilities Query { get; set; } = new();
    public UpdateCapabilities Update { get; set; } = new();
    public SubscribeCapabilities Subscribe { get; set; } = new();
}

public class QueryCapabilities
{
    public bool History { get; set; }
}

public class UpdateCapabilities
{
    public bool Current { get; set; }
    public bool History { get; set; }
}

public class SubscribeCapabilities
{
    public bool Stream { get; set; }
}
