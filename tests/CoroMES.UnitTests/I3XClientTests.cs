using System.Net;
using System.Text.Json;
using CoroMES.Industrial.i3X;

namespace CoroMES.UnitTests;

public class I3XClientTests
{
    [Fact]
    public async Task GetObjectsAsync_UsesStandardTypeIdFilterAndReadsRawArray()
    {
        var handler = new RecordingHandler(request =>
        {
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal("/v0/objects?typeId=work-unit-type&includeMetadata=true", request.RequestUri?.PathAndQuery);

            return JsonResponse("""
                [
                  {
                    "elementId": "pump-101",
                    "displayName": "pump-101",
                    "typeId": "work-unit-type",
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
