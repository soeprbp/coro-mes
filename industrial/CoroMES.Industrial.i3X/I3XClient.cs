using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
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
    Task<List<ObjectType>> GetObjectTypesByIdsAsync(List<string> elementIds, CancellationToken cancellationToken = default);
    Task<List<ObjectInstance>> GetObjectsAsync(string? typeId = null, bool includeMetadata = false, CancellationToken cancellationToken = default);
    Task<Dictionary<string, ObjectInstance>> GetObjectsByIdsAsync(List<string> elementIds, bool includeMetadata = false, CancellationToken cancellationToken = default);
    Task<Dictionary<string, ValueReadResult>> GetObjectValuesAsync(List<string> elementIds, int maxDepth = 1, CancellationToken cancellationToken = default);
    Task<Dictionary<string, ValueReadResult>> GetObjectHistoryAsync(List<string> elementIds, DateTime? startTime = null, DateTime? endTime = null, int maxDepth = 1, CancellationToken cancellationToken = default);
    Task<bool> WriteObjectValueAsync(string elementId, object value, string? quality = null, DateTime? timestamp = null, CancellationToken cancellationToken = default);
    Task<bool> WriteObjectHistoryAsync(string elementId, List<Vqt<object>> values, CancellationToken cancellationToken = default);
    Task<List<RelationshipType>> GetRelationshipTypesAsync(string? namespaceUri = null, CancellationToken cancellationToken = default);
    Task<List<RelationshipType>> GetRelationshipTypesByIdsAsync(List<string> elementIds, CancellationToken cancellationToken = default);
    Task<Dictionary<string, List<ObjectInstance>>> GetRelatedObjectsAsync(List<string> elementIds, string? relationshipType = null, bool includeMetadata = false, CancellationToken cancellationToken = default);
    Task<SubscriptionsResponse?> GetSubscriptionsAsync(CancellationToken cancellationToken = default);
    Task<CreateSubscriptionResponse?> CreateSubscriptionAsync(CancellationToken cancellationToken = default);
    Task<JsonElement?> GetSubscriptionAsync(string subscriptionId, CancellationToken cancellationToken = default);
    Task<bool> DeleteSubscriptionAsync(string subscriptionId, CancellationToken cancellationToken = default);
    Task<bool> RegisterSubscriptionItemsAsync(string subscriptionId, List<string> elementIds, int? maxDepth = 1, CancellationToken cancellationToken = default);
    Task<bool> UnregisterSubscriptionItemsAsync(string subscriptionId, List<string> elementIds, int? maxDepth = 1, CancellationToken cancellationToken = default);
    Task<JsonElement?> SyncSubscriptionAsync(string subscriptionId, CancellationToken cancellationToken = default);
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
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<ServerInfo?> GetServerInfoAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("info", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await ReadObjectAsync<ServerInfo>(response, cancellationToken);
    }

    public async Task<List<Namespace>> GetNamespacesAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("namespaces", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await ReadListAsync<Namespace>(response, cancellationToken);
    }

    public async Task<List<ObjectType>> GetObjectTypesAsync(string? namespaceUri = null, CancellationToken cancellationToken = default)
    {
        var url = namespaceUri != null ? $"objecttypes?namespaceUri={Uri.EscapeDataString(namespaceUri)}" : "objecttypes";
        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await ReadListAsync<ObjectType>(response, cancellationToken);
    }

    public async Task<List<ObjectType>> GetObjectTypesByIdsAsync(List<string> elementIds, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("objecttypes/query", new { elementIds }, _jsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await ReadListAsync<ObjectType>(response, cancellationToken);
    }

    public async Task<List<ObjectInstance>> GetObjectsAsync(string? typeId = null, bool includeMetadata = false, CancellationToken cancellationToken = default)
    {
        var queryParts = new List<string>();
        if (typeId != null) queryParts.Add($"typeId={Uri.EscapeDataString(typeId)}");
        if (includeMetadata) queryParts.Add("includeMetadata=true");

        var queryString = queryParts.Count > 0 ? "?" + string.Join("&", queryParts) : "";
        var response = await _httpClient.GetAsync($"objects{queryString}", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await ReadListAsync<ObjectInstance>(response, cancellationToken);
    }

    public async Task<Dictionary<string, ObjectInstance>> GetObjectsByIdsAsync(List<string> elementIds, bool includeMetadata = false, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("objects/list", new { elementIds, includeMetadata }, _jsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();

        var objects = await ReadListAsync<ObjectInstance>(response, cancellationToken);
        return objects.ToDictionary(o => o.ElementId, o => o);
    }

    public async Task<Dictionary<string, ValueReadResult>> GetObjectValuesAsync(List<string> elementIds, int maxDepth = 1, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("objects/value", new { elementIds, maxDepth }, _jsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await ReadDictionaryAsync<ValueReadResult>(response, cancellationToken);
    }

    public async Task<Dictionary<string, ValueReadResult>> GetObjectHistoryAsync(List<string> elementIds, DateTime? startTime = null, DateTime? endTime = null, int maxDepth = 1, CancellationToken cancellationToken = default)
    {
        var request = new
        {
            elementIds,
            startTime = startTime?.ToString("o"),
            endTime = endTime?.ToString("o"),
            maxDepth
        };

        var response = await _httpClient.PostAsJsonAsync("objects/history", request, _jsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await ReadDictionaryAsync<ValueReadResult>(response, cancellationToken);
    }

    public async Task<bool> WriteObjectValueAsync(string elementId, object value, string? quality = null, DateTime? timestamp = null, CancellationToken cancellationToken = default)
    {
        object body = timestamp.HasValue || !string.IsNullOrWhiteSpace(quality)
            ? new { value, quality = quality ?? "GOOD", timestamp = timestamp?.ToString("o") }
            : value;

        var response = await _httpClient.PutAsJsonAsync($"objects/{Uri.EscapeDataString(elementId)}/value", body, _jsonOptions, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> WriteObjectHistoryAsync(string elementId, List<Vqt<object>> values, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"objects/{Uri.EscapeDataString(elementId)}/history", values, _jsonOptions, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<List<RelationshipType>> GetRelationshipTypesAsync(string? namespaceUri = null, CancellationToken cancellationToken = default)
    {
        var url = namespaceUri != null ? $"relationshiptypes?namespaceUri={Uri.EscapeDataString(namespaceUri)}" : "relationshiptypes";
        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await ReadListAsync<RelationshipType>(response, cancellationToken);
    }

    public async Task<List<RelationshipType>> GetRelationshipTypesByIdsAsync(List<string> elementIds, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("relationshiptypes/query", new { elementIds }, _jsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await ReadListAsync<RelationshipType>(response, cancellationToken);
    }

    public async Task<Dictionary<string, List<ObjectInstance>>> GetRelatedObjectsAsync(List<string> elementIds, string? relationshipType = null, bool includeMetadata = false, CancellationToken cancellationToken = default)
    {
        var request = new RelatedObjectsRequest(elementIds, relationshipType, includeMetadata);
        var response = await _httpClient.PostAsJsonAsync("objects/related", request, _jsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await ReadRelatedObjectsAsync(response, elementIds, cancellationToken);
    }

    public async Task<SubscriptionsResponse?> GetSubscriptionsAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("subscriptions", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await ReadObjectAsync<SubscriptionsResponse>(response, cancellationToken);
    }

    public async Task<CreateSubscriptionResponse?> CreateSubscriptionAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("subscriptions", new { }, _jsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await ReadObjectAsync<CreateSubscriptionResponse>(response, cancellationToken);
    }

    public async Task<JsonElement?> GetSubscriptionAsync(string subscriptionId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"subscriptions/{Uri.EscapeDataString(subscriptionId)}", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await ReadObjectAsync<JsonElement>(response, cancellationToken);
    }

    public async Task<bool> DeleteSubscriptionAsync(string subscriptionId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.DeleteAsync($"subscriptions/{Uri.EscapeDataString(subscriptionId)}", cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public Task<bool> RegisterSubscriptionItemsAsync(string subscriptionId, List<string> elementIds, int? maxDepth = 1, CancellationToken cancellationToken = default)
    {
        return UpdateSubscriptionItemsAsync(subscriptionId, "register", elementIds, maxDepth, cancellationToken);
    }

    public Task<bool> UnregisterSubscriptionItemsAsync(string subscriptionId, List<string> elementIds, int? maxDepth = 1, CancellationToken cancellationToken = default)
    {
        return UpdateSubscriptionItemsAsync(subscriptionId, "unregister", elementIds, maxDepth, cancellationToken);
    }

    public async Task<JsonElement?> SyncSubscriptionAsync(string subscriptionId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync($"subscriptions/{Uri.EscapeDataString(subscriptionId)}/sync", new { }, _jsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await ReadObjectAsync<JsonElement>(response, cancellationToken);
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

    private async Task<bool> UpdateSubscriptionItemsAsync(string subscriptionId, string action, List<string> elementIds, int? maxDepth, CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"subscriptions/{Uri.EscapeDataString(subscriptionId)}/{action}",
            new { elementIds, maxDepth },
            _jsonOptions,
            cancellationToken);

        return response.IsSuccessStatusCode;
    }

    private async Task<T?> ReadObjectAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var root = UnwrapStandardResponse(document.RootElement);
        return root.Deserialize<T>(_jsonOptions);
    }

    private async Task<List<T>> ReadListAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var root = UnwrapStandardResponse(document.RootElement);

        if (root.ValueKind == JsonValueKind.Array)
        {
            return DeserializeList<T>(root);
        }

        if (root.ValueKind == JsonValueKind.Object)
        {
            return new List<T> { root.Deserialize<T>(_jsonOptions)! };
        }

        return new();
    }

    private async Task<Dictionary<string, T>> ReadDictionaryAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var root = UnwrapStandardResponse(document.RootElement);

        if (root.ValueKind == JsonValueKind.Object)
        {
            return root.Deserialize<Dictionary<string, T>>(_jsonOptions) ?? new();
        }

        if (root.ValueKind == JsonValueKind.Array)
        {
            return DeserializeDictionary<T>(root);
        }

        return new();
    }

    private async Task<Dictionary<string, List<ObjectInstance>>> ReadRelatedObjectsAsync(HttpResponseMessage response, List<string> elementIds, CancellationToken cancellationToken)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var root = UnwrapStandardResponse(document.RootElement);

        if (root.ValueKind != JsonValueKind.Array)
        {
            return new();
        }

        if (root.EnumerateArray().FirstOrDefault() is { ValueKind: JsonValueKind.Object } first &&
            first.TryGetProperty("elementId", out _) &&
            first.TryGetProperty("result", out _))
        {
            var dict = new Dictionary<string, List<ObjectInstance>>();
            foreach (var item in root.EnumerateArray())
            {
                if (!item.TryGetProperty("elementId", out var elementIdProperty) ||
                    !item.TryGetProperty("result", out var result))
                {
                    continue;
                }

                dict[elementIdProperty.GetString() ?? string.Empty] = DeserializeRelatedObjects(result);
            }

            return dict;
        }

        var relatedObjects = DeserializeList<ObjectInstance>(root);
        return elementIds.ToDictionary(elementId => elementId, _ => relatedObjects);
    }

    private List<ObjectInstance> DeserializeRelatedObjects(JsonElement result)
    {
        if (result.ValueKind != JsonValueKind.Array)
        {
            return new();
        }

        var objects = new List<ObjectInstance>();
        foreach (var item in result.EnumerateArray())
        {
            var objectElement = item.ValueKind == JsonValueKind.Object && item.TryGetProperty("object", out var nestedObject)
                ? nestedObject
                : item;

            if (objectElement.Deserialize<ObjectInstance>(_jsonOptions) is { } relatedObject)
            {
                objects.Add(relatedObject);
            }
        }

        return objects;
    }

    private List<T> DeserializeList<T>(JsonElement root)
    {
        var items = new List<T>();
        foreach (var item in root.EnumerateArray())
        {
            var value = item;
            if (item.ValueKind == JsonValueKind.Object && item.TryGetProperty("result", out var result))
            {
                value = result;
            }

            if (value.Deserialize<T>(_jsonOptions) is { } deserialized)
            {
                items.Add(deserialized);
            }
        }

        return items;
    }

    private Dictionary<string, T> DeserializeDictionary<T>(JsonElement root)
    {
        var dict = new Dictionary<string, T>();
        foreach (var item in root.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object ||
                !item.TryGetProperty("elementId", out var elementIdProperty))
            {
                continue;
            }

            var value = item.TryGetProperty("result", out var result) ? result : item;
            var elementId = elementIdProperty.GetString();
            if (!string.IsNullOrWhiteSpace(elementId) && value.Deserialize<T>(_jsonOptions) is { } deserialized)
            {
                dict[elementId] = deserialized;
            }
        }

        return dict;
    }

    private static JsonElement UnwrapStandardResponse(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("success", out _))
        {
            return root;
        }

        if (root.TryGetProperty("result", out var result))
        {
            return result;
        }

        if (root.TryGetProperty("results", out var results))
        {
            return results;
        }

        return root;
    }

    private sealed record RelatedObjectsRequest(
        List<string> ElementIds,
        [property: JsonPropertyName("relationshiptype")] string? RelationshipType,
        bool IncludeMetadata);
}
