using System.Net;
using System.Text.Json;
using CoroMES.Industrial.i3X;

namespace CoroMES.UnitTests;

public class I3XClientTests
{
    [Fact]
    public async Task GetObjectsAsync_UsesStandardTypeElementIdFilterAndReadsRawArray()
    {
        var handler = new RecordingHandler(request =>
        {
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal("/v0/objects?typeElementId=work-unit-type&includeMetadata=true", request.RequestUri?.PathAndQuery);

            return JsonResponse("""
                [
                  {
                    "elementId": "pump-101",
                    "displayName": "pump-101",
                    "typeElementId": "work-unit-type",
                    "parentId": "pump-station",
                    "isComposition": true,
                    "namespaceUri": "https://isa.org/isa95"
                  }
                ]
                """);
        });

        var client = CreateClient(handler);

        var objects = await client.GetObjectsAsync(typeId: "work-unit-type", includeMetadata: true);

        Assert.Single(objects);
        Assert.Equal("pump-101", objects[0].ElementId);
        Assert.Equal("work-unit-type", objects[0].TypeId);
    }

    [Fact]
    public async Task WriteObjectValueAsync_UsesBulkCurrentValueRoute()
    {
        var handler = new RecordingHandler(async request =>
        {
            Assert.Equal(HttpMethod.Put, request.Method);
            Assert.Equal("/v0/objects/value", request.RequestUri?.PathAndQuery);

            var body = await request.Content!.ReadAsStringAsync();
            Assert.Contains("\"updates\":[", body);
            Assert.Contains("\"elementId\":\"pump-101\"", body);
            Assert.Contains("\"value\":12.5", body);
            Assert.Contains("\"quality\":\"Good\"", body);

            return JsonResponse("""
                {
                  "success": true,
                  "results": [
                    { "success": true, "elementId": "pump-101", "result": null }
                  ]
                }
                """);
        });

        var client = CreateClient(handler);

        var written = await client.WriteObjectValueAsync("pump-101", 12.5);

        Assert.True(written);
    }

    [Fact]
    public async Task WriteObjectHistoryAsync_UsesBulkHistoryRoute()
    {
        var handler = new RecordingHandler(async request =>
        {
            Assert.Equal(HttpMethod.Put, request.Method);
            Assert.Equal("/v0/objects/history", request.RequestUri?.PathAndQuery);

            var body = await request.Content!.ReadAsStringAsync();
            Assert.Contains("\"updates\":[", body);
            Assert.Contains("\"elementId\":\"pump-101\"", body);
            Assert.Contains("\"quality\":\"Good\"", body);
            Assert.Contains("\"timestamp\":\"2026-06-10T12:00:00", body);

            return JsonResponse("""
                {
                  "success": true,
                  "results": [
                    { "success": true, "elementId": "pump-101", "result": null }
                  ]
                }
                """);
        });

        var client = CreateClient(handler);

        var written = await client.WriteObjectHistoryAsync(
            "pump-101",
            [
                new()
                {
                    Value = 11.5,
                    Quality = "Good",
                    Timestamp = new DateTime(2026, 6, 10, 12, 0, 0, DateTimeKind.Utc)
                }
            ]);

        Assert.True(written);
    }

    [Fact]
    public async Task GetObjectValuesAsync_PostsStandardRouteAndReadsDataDictionary()
    {
        var handler = new RecordingHandler(async request =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("/v0/objects/value", request.RequestUri?.PathAndQuery);

            var body = await request.Content!.ReadAsStringAsync();
            Assert.Contains("\"elementIds\":[\"pump-101\"]", body);
            Assert.Contains("\"maxDepth\":1", body);

            return JsonResponse("""
                {
                  "pump-101": {
                    "data": [
                      {
                        "value": 12.5,
                        "quality": "GOOD",
                        "timestamp": "2026-05-28T14:10:55Z"
                      }
                    ]
                  }
                }
                """);
        });

        var client = CreateClient(handler);

        var values = await client.GetObjectValuesAsync(new List<string> { "pump-101" });

        Assert.True(values.ContainsKey("pump-101"));
        Assert.Equal("GOOD", values["pump-101"].Value.Quality);
        Assert.Equal(12.5, ((JsonElement)values["pump-101"].Value.Value!).GetDouble());
    }

    [Fact]
    public async Task GetObjectValuesAsync_ReadsWrappedPerElementResults()
    {
        var handler = new RecordingHandler(_ => JsonResponse("""
            {
              "success": true,
              "results": [
                {
                  "elementId": "system",
                  "success": true,
                  "result": {
                    "isComposition": false,
                    "quality": "Good",
                    "timestamp": "2026-05-28T14:45:32Z",
                    "value": {
                      "systemState": "Monitoring"
                    }
                  }
                }
              ]
            }
            """));

        var client = CreateClient(handler);

        var values = await client.GetObjectValuesAsync(new List<string> { "system" });

        Assert.True(values.ContainsKey("system"));
        Assert.Equal("Good", values["system"].Value.Quality);
        Assert.Equal("Monitoring", ((JsonElement)values["system"].Value.Value!).GetProperty("systemState").GetString());
    }

    [Fact]
    public async Task GetObjectHistoryAsync_ReadsWrappedValuesArray()
    {
        var handler = new RecordingHandler(_ => JsonResponse("""
            {
              "success": true,
              "results": [
                {
                  "elementId": "events",
                  "success": true,
                  "result": {
                    "isComposition": false,
                    "values": [
                      {
                        "quality": "Good",
                        "timestamp": "2026-05-28T10:44:55.96749",
                        "value": {
                          "motionPercent": 8.022135416666666,
                          "status": "motion"
                        }
                      }
                    ]
                  }
                }
              ]
            }
            """));

        var client = CreateClient(handler);

        var history = await client.GetObjectHistoryAsync(new List<string> { "events" });

        Assert.Single(history["events"].Values);
        Assert.Equal("motion", ((JsonElement)history["events"].Values[0].Value!).GetProperty("status").GetString());
    }

    [Fact]
    public async Task GetObjectsByIdsAsync_ReadsWrappedResultsAndTypeElementId()
    {
        var handler = new RecordingHandler(_ => JsonResponse("""
            {
              "success": true,
              "results": [
                {
                  "elementId": "camera-0",
                  "success": true,
                  "result": {
                    "elementId": "camera-0",
                    "displayName": "Camera USB webcam #0",
                    "typeElementId": "Camera",
                    "parentId": "MES-Vision",
                    "isComposition": true
                  }
                }
              ]
            }
            """));

        var client = CreateClient(handler);

        var objects = await client.GetObjectsByIdsAsync(new List<string> { "camera-0" });

        Assert.Equal("Camera", objects["camera-0"].TypeId);
        Assert.Equal("Camera", objects["camera-0"].TypeElementId);
    }

    [Fact]
    public async Task GetRelatedObjectsAsync_UsesOpenApiRelationshipTypePropertyName()
    {
        var handler = new RecordingHandler(async request =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("/v0/objects/related", request.RequestUri?.PathAndQuery);

            var body = await request.Content!.ReadAsStringAsync();
            Assert.Contains("\"relationshiptype\":\"HasComponent\"", body);
            Assert.DoesNotContain("\"relationshipType\"", body);

            return JsonResponse("""
                [
                  {
                    "elementId": "pump-101-state",
                    "displayName": "pump-101 State",
                    "typeId": "state-type",
                    "parentId": "pump-101",
                    "isComposition": false,
                    "namespaceUri": "https://abelara.com/equipment"
                  }
                ]
                """);
        });

        var client = CreateClient(handler);

        var related = await client.GetRelatedObjectsAsync(
            new List<string> { "pump-101" },
            relationshipType: "HasComponent");

        Assert.Single(related["pump-101"]);
        Assert.Equal("pump-101-state", related["pump-101"][0].ElementId);
    }

    [Fact]
    public async Task GetRelatedObjectsAsync_ReadsWrappedNestedObjectResults()
    {
        var handler = new RecordingHandler(_ => JsonResponse("""
            {
              "success": true,
              "results": [
                {
                  "elementId": "camera-0",
                  "success": true,
                  "result": [
                    {
                      "sourceRelationship": "HasEvents",
                      "object": {
                        "elementId": "events",
                        "displayName": "Motion Events",
                        "typeElementId": "MotionEvent",
                        "parentId": "camera-0",
                        "isComposition": false
                      }
                    }
                  ]
                }
              ]
            }
            """));

        var client = CreateClient(handler);

        var related = await client.GetRelatedObjectsAsync(new List<string> { "camera-0" });

        Assert.Single(related["camera-0"]);
        Assert.Equal("events", related["camera-0"][0].ElementId);
        Assert.Equal("MotionEvent", related["camera-0"][0].TypeId);
    }

    [Fact]
    public async Task SubscriptionMethods_UseBodyOrientedClientScopedRoutes()
    {
        var seen = new List<(HttpMethod Method, string PathAndQuery, string Body)>();
        var handler = new RecordingHandler(async request =>
        {
            seen.Add((
                request.Method,
                request.RequestUri?.PathAndQuery ?? string.Empty,
                request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync()));

            return JsonResponse("""
                {
                  "success": true,
                  "result": {
                    "subscriptionId": "sub-1",
                    "displayName": "MES Vision Collector"
                  }
                }
                """);
        });

        var client = CreateClient(handler);

        await client.CreateSubscriptionAsync("coromes", "MES Vision Collector");
        await client.RegisterSubscriptionItemsAsync("coromes", "sub-1", ["camera-0"], maxDepth: 1);
        await client.SyncSubscriptionAsync("coromes", "sub-1", lastSequenceNumber: 42);
        await client.DeleteSubscriptionAsync("coromes", "sub-1");

        Assert.Collection(
            seen,
            create =>
            {
                Assert.Equal(HttpMethod.Post, create.Method);
                Assert.Equal("/v0/subscriptions", create.PathAndQuery);
                Assert.Contains("\"clientId\":\"coromes\"", create.Body);
                Assert.Contains("\"displayName\":\"MES Vision Collector\"", create.Body);
            },
            register =>
            {
                Assert.Equal(HttpMethod.Post, register.Method);
                Assert.Equal("/v0/subscriptions/register", register.PathAndQuery);
                Assert.Contains("\"clientId\":\"coromes\"", register.Body);
                Assert.Contains("\"subscriptionId\":\"sub-1\"", register.Body);
                Assert.Contains("\"elementIds\":[\"camera-0\"]", register.Body);
            },
            sync =>
            {
                Assert.Equal(HttpMethod.Post, sync.Method);
                Assert.Equal("/v0/subscriptions/sync", sync.PathAndQuery);
                Assert.Contains("\"clientId\":\"coromes\"", sync.Body);
                Assert.Contains("\"subscriptionId\":\"sub-1\"", sync.Body);
                Assert.Contains("\"lastSequenceNumber\":42", sync.Body);
            },
            delete =>
            {
                Assert.Equal(HttpMethod.Post, delete.Method);
                Assert.Equal("/v0/subscriptions/delete", delete.PathAndQuery);
                Assert.Contains("\"clientId\":\"coromes\"", delete.Body);
                Assert.Contains("\"subscriptionIds\":[\"sub-1\"]", delete.Body);
            });
    }

    private static I3XClient CreateClient(HttpMessageHandler handler)
    {
        return new I3XClient(new HttpClient(handler)
        {
            BaseAddress = new Uri("https://example.test/v0/")
        });
    }

    private static HttpResponseMessage JsonResponse(string json)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _handler;

        public RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = request => Task.FromResult(handler(request));
        }

        public RecordingHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return _handler(request);
        }
    }
}
