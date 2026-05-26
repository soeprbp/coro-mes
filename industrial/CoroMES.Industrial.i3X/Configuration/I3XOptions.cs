namespace CoroMES.Industrial.i3X.Configuration;

/// <summary>
/// Configuration options for i3X client.
/// </summary>
public class I3XOptions
{
    /// <summary>
    /// Base URL of the i3X server (e.g., https://i3x.example.com/v1)
    /// </summary>
    public string BaseUrl { get; set; } = "http://localhost:8080/v1";

    /// <summary>
    /// Optional API key for authentication.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Timeout for HTTP requests in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Whether to register MES object types on startup.
    /// </summary>
    public bool AutoRegisterTypes { get; set; } = true;

    /// <summary>
    /// i3X namespace URI to use for MES objects.
    /// </summary>
    public string NamespaceUri { get; set; } = ObjectTypes.MesObjectTypes.NamespaceUri;
}
