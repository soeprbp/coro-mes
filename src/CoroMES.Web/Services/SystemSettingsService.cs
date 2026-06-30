using CoroMES.Core.Entities;
using CoroMES.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CoroMES.Web.Services;

public interface ISystemSettingsService
{
    Task<SystemSettingsSnapshot> GetSnapshotAsync(string userId, CancellationToken cancellationToken = default);
    Task<SystemSettingsSnapshot> SaveAsync(SystemSettingsUpdateRequest request, string actor, CancellationToken cancellationToken = default);
}

public sealed class SystemSettingsService(ApplicationDbContext db, IConfiguration configuration, IWebHostEnvironment environment) : ISystemSettingsService
{
    private const string IntegrationsCategory = "IntegrationEndpoints";
    private const string ProtocolsCategory = "ProtocolSettings";
    private const string FeatureFlagsCategory = "FeatureFlags";

    private static readonly IntegrationEndpointSettingDto[] DefaultIntegrationEndpoints =
    [
        new("MES-Vision i3X", "Integration.MesVisionI3X.BaseUrl", "Integration.MesVisionI3X.Mode", "https://rocktumbler.57446516.xyz/i3x/v1/", "read-only", "https://rocktumbler.57446516.xyz/i3x/v1/"),
        new("MES-Vision Dashboard", "Integration.MesVisionDashboard.BaseUrl", "Integration.MesVisionDashboard.Mode", "https://rocktumbler.57446516.xyz/", "disabled", "https://rocktumbler.57446516.xyz/"),
        new("UpKeep", "Integration.Upkeep.BaseUrl", "Integration.Upkeep.Mode", "", "mock", "https://api.onupkeep.com/")
    ];

    private static readonly ProtocolSettingDto[] DefaultProtocolSettings =
    [
        new("MQTT", "Protocol.Mqtt.Enabled", "Protocol.Mqtt.Host", "Protocol.Mqtt.Port", true, "localhost", 1883),
        new("OPC-UA", "Protocol.OpcUa.Enabled", "Protocol.OpcUa.Host", "Protocol.OpcUa.Port", false, "opc.tcp://localhost", 4840),
        new("Ethernet/IP", "Protocol.EthernetIp.Enabled", "Protocol.EthernetIp.Host", "Protocol.EthernetIp.Port", false, "192.168.1.10", 44818)
    ];

    private static readonly FeatureFlagSettingDto[] DefaultFeatureFlags =
    [
        new("Blazor display persistence", "Feature.BlazorDisplayPersistence", "Save shop-floor display definitions in CoroMES.", true),
        new("MES-Vision collector", "Feature.MesVisionCollector", "Enable the future read-only vision telemetry collector.", false),
        new("Alarm alerting", "Feature.AlarmAlerting", "Route alarm events to configured notification channels.", true),
        new("i3X repository mode", "Feature.I3XRepositoryMode", "Use i3X-backed repositories instead of EF-backed repositories.", false),
        new("Enterprise identity", "Feature.EnterpriseIdentity", "Replace the migration access-code gate.", false)
    ];

    public async Task<SystemSettingsSnapshot> GetSnapshotAsync(string userId, CancellationToken cancellationToken = default)
    {
        var normalizedUserId = NormalizeUserId(userId);
        var values = await LoadValuesAsync(normalizedUserId, cancellationToken);

        return new SystemSettingsSnapshot(
            normalizedUserId,
            DefaultIntegrationEndpoints.Select(endpoint => new IntegrationEndpointSettingDto(
                endpoint.Name,
                endpoint.BaseUrlKey,
                endpoint.ModeKey,
                Get(values, endpoint.BaseUrlKey, endpoint.BaseUrl),
                NormalizeMode(Get(values, endpoint.ModeKey, endpoint.Mode)),
                endpoint.Placeholder)).ToList(),
            DefaultProtocolSettings.Select(protocol => new ProtocolSettingDto(
                protocol.Name,
                protocol.EnabledKey,
                protocol.HostKey,
                protocol.PortKey,
                GetBool(values, protocol.EnabledKey, protocol.Enabled),
                Get(values, protocol.HostKey, protocol.Host),
                GetInt(values, protocol.PortKey, protocol.Port, 1, 65535))).ToList(),
            DefaultFeatureFlags.Select(flag => new FeatureFlagSettingDto(
                flag.Name,
                flag.Key,
                flag.Description,
                GetBool(values, flag.Key, flag.Enabled))).ToList(),
            GetSecretStatuses());
    }

    public async Task<SystemSettingsSnapshot> SaveAsync(SystemSettingsUpdateRequest request, string actor, CancellationToken cancellationToken = default)
    {
        var normalizedUserId = NormalizeUserId(actor);
        var existing = await db.SystemSettings
            .Where(setting => setting.UserId == normalizedUserId)
            .ToDictionaryAsync(setting => setting.Key, cancellationToken);
        var now = DateTime.UtcNow;

        foreach (var endpoint in MergeIntegrationEndpoints(request.IntegrationEndpoints))
        {
            var baseUrl = CleanUrl(endpoint.BaseUrl, endpoint.Placeholder);
            RejectSecretLikeValue(endpoint.BaseUrlKey, baseUrl);
            Upsert(existing, endpoint.BaseUrlKey, IntegrationsCategory, baseUrl, $"Base URL for {endpoint.Name}.", actor, now);
            Upsert(existing, endpoint.ModeKey, IntegrationsCategory, NormalizeMode(endpoint.Mode), $"Mode for {endpoint.Name}.", actor, now);
        }

        foreach (var protocol in MergeProtocolSettings(request.ProtocolSettings))
        {
            var host = Clean(protocol.Host, 300);
            RejectSecretLikeValue(protocol.HostKey, host);
            Upsert(existing, protocol.EnabledKey, ProtocolsCategory, protocol.Enabled.ToString(), $"Enable {protocol.Name}.", actor, now);
            Upsert(existing, protocol.HostKey, ProtocolsCategory, host, $"Host for {protocol.Name}.", actor, now);
            Upsert(existing, protocol.PortKey, ProtocolsCategory, Math.Clamp(protocol.Port, 1, 65535).ToString(), $"Port for {protocol.Name}.", actor, now);
        }

        foreach (var flag in MergeFeatureFlags(request.FeatureFlags))
        {
            Upsert(existing, flag.Key, FeatureFlagsCategory, flag.Enabled.ToString(), flag.Description, actor, now);
        }

        await db.SaveChangesAsync(cancellationToken);
        return await GetSnapshotAsync(normalizedUserId, cancellationToken);
    }

    private async Task<Dictionary<string, string>> LoadValuesAsync(string userId, CancellationToken cancellationToken)
    {
        var settings = await db.SystemSettings
            .AsNoTracking()
            .Where(setting => setting.UserId == null || setting.UserId == userId)
            .OrderBy(setting => setting.UserId == null ? 0 : 1)
            .ToListAsync(cancellationToken);

        return settings
            .GroupBy(setting => setting.Key)
            .ToDictionary(group => group.Key, group => group.Last().Value);
    }

    private static IEnumerable<IntegrationEndpointSettingDto> MergeIntegrationEndpoints(IReadOnlyList<IntegrationEndpointSettingDto>? incoming)
    {
        return DefaultIntegrationEndpoints.Select(defaultEndpoint =>
        {
            var match = incoming?.FirstOrDefault(endpoint => endpoint.BaseUrlKey == defaultEndpoint.BaseUrlKey && endpoint.ModeKey == defaultEndpoint.ModeKey);
            return match is null
                ? defaultEndpoint
                : new IntegrationEndpointSettingDto(defaultEndpoint.Name, defaultEndpoint.BaseUrlKey, defaultEndpoint.ModeKey, match.BaseUrl, match.Mode, defaultEndpoint.Placeholder);
        });
    }

    private static IEnumerable<ProtocolSettingDto> MergeProtocolSettings(IReadOnlyList<ProtocolSettingDto>? incoming)
    {
        return DefaultProtocolSettings.Select(defaultProtocol =>
        {
            var match = incoming?.FirstOrDefault(protocol => protocol.EnabledKey == defaultProtocol.EnabledKey);
            return match is null
                ? defaultProtocol
                : new ProtocolSettingDto(defaultProtocol.Name, defaultProtocol.EnabledKey, defaultProtocol.HostKey, defaultProtocol.PortKey, match.Enabled, match.Host, match.Port);
        });
    }

    private static IEnumerable<FeatureFlagSettingDto> MergeFeatureFlags(IReadOnlyList<FeatureFlagSettingDto>? incoming)
    {
        return DefaultFeatureFlags.Select(defaultFlag =>
        {
            var match = incoming?.FirstOrDefault(flag => flag.Key == defaultFlag.Key);
            return match is null
                ? defaultFlag
                : new FeatureFlagSettingDto(defaultFlag.Name, defaultFlag.Key, defaultFlag.Description, match.Enabled);
        });
    }

    private void Upsert(
        Dictionary<string, SystemSetting> existing,
        string key,
        string category,
        string value,
        string description,
        string actor,
        DateTime now)
    {
        if (existing.TryGetValue(key, out var setting))
        {
            if (setting.Value == value && setting.Description == description)
            {
                return;
            }

            setting.Value = value;
            setting.Description = description;
            setting.UpdatedAt = now;
            setting.UpdatedBy = actor;
            return;
        }

        var created = new SystemSetting
        {
            UserId = NormalizeUserId(actor),
            Key = key,
            Category = category,
            Value = value,
            Description = description,
            CreatedAt = now,
            CreatedBy = actor
        };

        existing[key] = created;
        db.SystemSettings.Add(created);
    }

    private static string NormalizeUserId(string? userId)
    {
        var normalized = Clean(userId, 120);
        return string.IsNullOrWhiteSpace(normalized) ? "unknown" : normalized;
    }

    private IReadOnlyList<SecretStatusDto> GetSecretStatuses()
    {
        return
        [
            new("Admin access code", "Auth:AdminAccessCode", HasConfiguredValue("Auth:AdminAccessCode") || environment.IsDevelopment(), environment.IsDevelopment() && !HasConfiguredValue("Auth:AdminAccessCode") ? "development fallback" : "configured"),
            new("UpKeep API key", "Upkeep:ApiKey", HasConfiguredValue("Upkeep:ApiKey"), "environment/user secrets"),
            new("i3X API key", "i3x:apiKey", HasConfiguredValue("i3x:apiKey"), "environment/user secrets"),
            new("Alert provider credentials", "Alerting:*", HasAnyConfiguredValue("Alerting:EmailApiKey", "Alerting:SmsApiKey", "Alerting:PushoverToken", "Alerting:UpkeepApiKey"), "environment/user secrets")
        ];
    }

    private bool HasConfiguredValue(string key)
    {
        return !string.IsNullOrWhiteSpace(configuration[key]);
    }

    private bool HasAnyConfiguredValue(params string[] keys)
    {
        return keys.Any(HasConfiguredValue);
    }

    private static string Get(Dictionary<string, string> values, string key, string fallback)
    {
        return values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : fallback;
    }

    private static bool GetBool(Dictionary<string, string> values, string key, bool fallback)
    {
        return values.TryGetValue(key, out var value) && bool.TryParse(value, out var result) ? result : fallback;
    }

    private static int GetInt(Dictionary<string, string> values, string key, int fallback, int min, int max)
    {
        return values.TryGetValue(key, out var value) && int.TryParse(value, out var result)
            ? Math.Clamp(result, min, max)
            : fallback;
    }

    private static string Clean(string? value, int maxLength)
    {
        var cleaned = value?.Trim() ?? string.Empty;
        return cleaned.Length <= maxLength ? cleaned : cleaned[..maxLength];
    }

    private static string CleanUrl(string? value, string fallback)
    {
        var cleaned = Clean(value, 300);
        if (string.IsNullOrWhiteSpace(cleaned))
        {
            return string.Empty;
        }

        if (!Uri.TryCreate(cleaned, UriKind.Absolute, out var uri))
        {
            return fallback;
        }

        if (uri.Scheme is not ("http" or "https"))
        {
            throw new InvalidOperationException("Only HTTP and HTTPS endpoint URLs can be saved in admin settings.");
        }

        return cleaned;
    }

    private static string NormalizeMode(string? mode)
    {
        var normalized = Clean(mode, 40).ToLowerInvariant();
        return normalized is "disabled" or "mock" or "read-only" or "enabled" or "live" ? normalized : "disabled";
    }

    private static void RejectSecretLikeValue(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        var combined = $"{key} {value}";
        var blockedPatterns = new[]
        {
            "apikey",
            "api_key",
            "bearer ",
            "connectionstring",
            "password",
            "secret",
            "token",
            "rtsp://"
        };

        if (blockedPatterns.Any(pattern => combined.Contains(pattern, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("Settings cannot store keys, tokens, passwords, connection strings, RTSP credentials, or other secret-like values.");
        }
    }
}

public sealed record SystemSettingsSnapshot(
    string UserId,
    IReadOnlyList<IntegrationEndpointSettingDto> IntegrationEndpoints,
    IReadOnlyList<ProtocolSettingDto> ProtocolSettings,
    IReadOnlyList<FeatureFlagSettingDto> FeatureFlags,
    IReadOnlyList<SecretStatusDto> SecretStatuses);

public sealed record SystemSettingsUpdateRequest(
    IReadOnlyList<IntegrationEndpointSettingDto>? IntegrationEndpoints,
    IReadOnlyList<ProtocolSettingDto>? ProtocolSettings,
    IReadOnlyList<FeatureFlagSettingDto>? FeatureFlags);

public sealed class IntegrationEndpointSettingDto
{
    public IntegrationEndpointSettingDto()
    {
    }

    public IntegrationEndpointSettingDto(string name, string baseUrlKey, string modeKey, string baseUrl, string mode, string placeholder)
    {
        Name = name;
        BaseUrlKey = baseUrlKey;
        ModeKey = modeKey;
        BaseUrl = baseUrl;
        Mode = mode;
        Placeholder = placeholder;
    }

    public string Name { get; set; } = string.Empty;
    public string BaseUrlKey { get; set; } = string.Empty;
    public string ModeKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string Mode { get; set; } = string.Empty;
    public string Placeholder { get; set; } = string.Empty;
}

public sealed class ProtocolSettingDto
{
    public ProtocolSettingDto()
    {
    }

    public ProtocolSettingDto(string name, string enabledKey, string hostKey, string portKey, bool enabled, string host, int port)
    {
        Name = name;
        EnabledKey = enabledKey;
        HostKey = hostKey;
        PortKey = portKey;
        Enabled = enabled;
        Host = host;
        Port = port;
    }

    public string Name { get; set; } = string.Empty;
    public string EnabledKey { get; set; } = string.Empty;
    public string HostKey { get; set; } = string.Empty;
    public string PortKey { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
}

public sealed class FeatureFlagSettingDto
{
    public FeatureFlagSettingDto()
    {
    }

    public FeatureFlagSettingDto(string name, string key, string description, bool enabled)
    {
        Name = name;
        Key = key;
        Description = description;
        Enabled = enabled;
    }

    public string Name { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool Enabled { get; set; }
}

public sealed class SecretStatusDto
{
    public SecretStatusDto()
    {
    }

    public SecretStatusDto(string name, string configurationKey, bool configured, string source)
    {
        Name = name;
        ConfigurationKey = configurationKey;
        Configured = configured;
        Source = source;
    }

    public string Name { get; set; } = string.Empty;
    public string ConfigurationKey { get; set; } = string.Empty;
    public bool Configured { get; set; }
    public string Source { get; set; } = string.Empty;
}
