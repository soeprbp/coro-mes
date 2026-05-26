using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Linq;
using CoroMES.Industrial.i3X.Models;

namespace CoroMES.Industrial.i3X;

/// <summary>
/// i3X client for interacting with i3X-compliant manufacturing data servers.
/// </summary>
public interface II3XClient
{
    Task<ServerInfo?> GetServerInfoAsync(CancellationToken cancellationToken = default);
    Task<List<Namespace>> GetNamespacesAsync(CancellationToken cancellationToken = default);
    Task<List<ObjectType>> GetObjectTypesAsync(string? namespaceUri = null, CancellationToken cancellationToken = default);
    Task<List<ObjectInstance>> GetObjectsAsync(string? typeElementId = null, bool includeMetadata = false, bool root = false, CancellationToken cancellationToken = default);
    Task<Dictionary<string, ObjectInstance>> GetObjectsByIdsAsync(List<string> elementIds, bool includeMetadata = false, CancellationToken cancellationToken = default);
    Task<Dictionary<string, ValueReadResult>> GetObjectValuesAsync(List<string> elementIds, int maxDepth = 1, CancellationToken cancellationToken = default);
    Task<Dictionary<string, List<ValueReadResult>>> GetObjectHistoryAsync(List<string> elementIds, DateTime startTime, DateTime endTime, int maxDepth = 1, CancellationToken cancellationToken = default);
    Task<bool> WriteObjectValueAsync(string elementId, object value, string? quality = null, DateTime? timestamp = null, CancellationToken cancellationToken = default);
    Task<List<RelationshipType>> GetRelationshipTypesAsync(string? namespaceUri = null, CancellationToken cancellationToken = default);
    Task<Dictionary<string, List<ObjectInstance>>> GetRelatedObjectsAsync(List<string> elementIds, string? relationshipType = null, bool includeMetadata = false, CancellationToken cancellationToken = default);
}

/// <summary>
/// i3X client implementation using HttpClient.
/// </summary>
public class I3XClient : II3XClient, IAsyncDisposable
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public I3XClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    public async Task<ServerInfo?> GetServerInfoAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetFromJsonAsync<StandardResponse<ServerInfo>>("info", _jsonOptions, cancellationToken);
        return response?.Success == true ? response.Result : null;
    }

    public async Task<List<Namespace>> GetNamespacesAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetFromJsonAsync<StandardResponse<List<Namespace>>>("namespaces", _jsonOptions, cancellationToken);
        return response?.Success == true ? response.Result ?? new() : new();
    }

    public async Task<List<ObjectType>> GetObjectTypesAsync(string? namespaceUri = null, CancellationToken cancellationToken = default)
    {
        var url = namespaceUri != null ? $"objecttypes?namespaceUri={Uri.EscapeDataString(namespaceUri)}" : "objecttypes";
        var response = await _httpClient.GetFromJsonAsync<StandardResponse<List<ObjectType>>>(url, _jsonOptions, cancellationToken);
        return response?.Success == true ? response.Result ?? new() : new();
    }

    public async Task<List<ObjectInstance>> GetObjectsAsync(string? typeElementId = null, bool includeMetadata = false, bool root = false, CancellationToken cancellationToken = default)
    {
        var queryParts = new List<string>();
        if (typeElementId != null) queryParts.Add($"typeElementId={Uri.EscapeDataString(typeElementId)}");
        if (includeMetadata) queryParts.Add("includeMetadata=true");
        if (root) queryParts.Add("root=true");

        var queryString = queryParts.Count > 0 ? "?" + string.Join("&", queryParts) : "";
        var response = await _httpClient.GetFromJsonAsync<StandardResponse<List<ObjectInstance>>>($"objects{queryString}", _jsonOptions, cancellationToken);
        return response?.Success == true ? response.Result ?? new() : new();
    }

    public async Task<Dictionary<string, ObjectInstance>> GetObjectsByIdsAsync(List<string> elementIds, bool includeMetadata = false, CancellationToken cancellationToken = default)
    {
        var request = new { elementIds, includeMetadata };
        var response = await _httpClient.PostAsJsonAsync("objects/list", request, _jsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();

        var resp = await response.Content.ReadFromJsonAsync<StandardResponse<ObjectQueryResult>>(_jsonOptions, cancellationToken);
        var dict = new Dictionary<string, ObjectInstance>();
        if (resp?.Success == true && resp.Results != null)
        {
            foreach (var item in resp.Results.Where(r => r.Result != null))
            {
                dict[item.ElementId] = item.Result!;
            }
        }
        return dict;
    }

    public async Task<Dictionary<string, ValueReadResult>> GetObjectValuesAsync(List<string> elementIds, int maxDepth = 1, CancellationToken cancellationToken = default)
    {
        var request = new { elementIds, maxDepth };
        var response = await _httpClient.PostAsJsonAsync("objects/value", request, _jsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();

        var resp = await response.Content.ReadFromJsonAsync<StandardResponse<ObjectsValueResponseItem>>(_jsonOptions, cancellationToken);
        var dict = new Dictionary<string, ValueReadResult>();
        if (resp?.Success == true && resp.Results != null)
        {
            foreach (var item in resp.Results.Where(r => r.Result != null))
            {
                dict[item.ElementId] = item.Result!;
            }
        }
        return dict;
    }

    public async Task<Dictionary<string, List<ValueReadResult>>> GetObjectHistoryAsync(List<string> elementIds, DateTime startTime, DateTime endTime, int maxDepth = 1, CancellationToken cancellationToken = default)
    {
        var request = new { elementIds, startTime = startTime.ToString("o"), endTime = endTime.ToString("o"), maxDepth };
        var response = await _httpClient.PostAsJsonAsync("objects/history", request, _jsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();

        var resp = await response.Content.ReadFromJsonAsync<StandardResponse<ObjectsHistoryResponseItem>>(_jsonOptions, cancellationToken);
        var dict = new Dictionary<string, List<ValueReadResult>>();
        if (resp?.Success == true && resp.Results != null)
        {
            foreach (var item in resp.Results)
            {
                dict[item.ElementId] = item.Result ?? new List<ValueReadResult>();
            }
        }
        return dict;
    }

    public async Task<bool> WriteObjectValueAsync(string elementId, object value, string? quality = null, DateTime? timestamp = null, CancellationToken cancellationToken = default)
    {
        var body = new
        {
            value,
            quality = quality ?? "Good",
            timestamp = timestamp?.ToString("o")
        };
        var response = await _httpClient.PutAsJsonAsync($"objects/{elementId}/value", body, _jsonOptions, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return false;
        }
        return true;
    }

    public async Task<List<RelationshipType>> GetRelationshipTypesAsync(string? namespaceUri = null, CancellationToken cancellationToken = default)
    {
        var url = namespaceUri != null ? $"relationshiptypes?namespaceUri={Uri.EscapeDataString(namespaceUri)}" : "relationshiptypes";
        var response = await _httpClient.GetFromJsonAsync<StandardResponse<List<RelationshipType>>>(url, _jsonOptions, cancellationToken);
        return response?.Success == true ? response.Result ?? new() : new();
    }

    public async Task<Dictionary<string, List<ObjectInstance>>> GetRelatedObjectsAsync(List<string> elementIds, string? relationshipType = null, bool includeMetadata = false, CancellationToken cancellationToken = default)
    {
        var request = new { elementIds, relationshipType, includeMetadata };
        var response = await _httpClient.PostAsJsonAsync("objects/related", request, _jsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();

        var resp = await response.Content.ReadFromJsonAsync<StandardResponse<RelatedObjectsQueryResult>>(_jsonOptions, cancellationToken);
        var dict = new Dictionary<string, List<ObjectInstance>>();
        if (resp?.Success == true && resp.Results != null)
        {
            foreach (var item in resp.Results)
            {
                dict[item.ElementId] = item.Result ?? new List<ObjectInstance>();
            }
        }
        return dict;
    }

    public async ValueTask DisposeAsync()
    {
        if (_httpClient is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
        }
        else
        {
            _httpClient.Dispose();
        }
    }
}
