using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CoroMES.Core.Entities;
using CoroMES.Core.Enums;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace CoroMES.IntegrationTests;

public class BlazorHostAuthAndAuditTests
{
    [Fact]
    public async Task Anonymous_user_can_reach_public_routes_but_not_admin_or_api_routes()
    {
        using var factory = new CoroMesWebFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var health = await client.GetAsync("/health");
        var viewer = await client.GetAsync("/displays/viewer?id=preview&type=oee");
        var admin = await client.GetAsync("/admin/equipment");
        var builder = await client.GetAsync("/displays/builder");
        var equipmentApi = await client.GetAsync("/api/v1/equipment");

        Assert.Equal(HttpStatusCode.OK, health.StatusCode);
        Assert.Equal(HttpStatusCode.OK, viewer.StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, admin.StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, builder.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, equipmentApi.StatusCode);
        Assert.Equal("/login", admin.Headers.Location?.AbsolutePath);
        Assert.Equal("/login", builder.Headers.Location?.AbsolutePath);
    }

    [Fact]
    public async Task Valid_admin_code_allows_admin_and_api_access()
    {
        using var factory = new CoroMesWebFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        var login = await SignInAsync(client);
        var admin = await client.GetAsync("/admin/equipment");
        var equipmentApi = await client.GetAsync("/api/v1/equipment");

        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
        Assert.Equal("/admin/equipment", login.Headers.Location?.OriginalString);
        Assert.Equal(HttpStatusCode.OK, admin.StatusCode);
        Assert.Equal(HttpStatusCode.OK, equipmentApi.StatusCode);
    }

    [Fact]
    public async Task Equipment_create_and_delete_write_audit_records()
    {
        using var factory = new CoroMesWebFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        await SignInAsync(client);

        var code = $"INT-{Guid.NewGuid():N}"[..12];
        var equipment = new Equipment
        {
            Code = code,
            Name = "Integration Test Machine",
            Type = EquipmentType.Machine,
            Status = EquipmentStatus.Available,
            Location = "Test Cell",
            Protocol = "manual"
        };

        var create = await client.PostAsJsonAsync("/api/v1/equipment", equipment);
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        var created = await create.Content.ReadFromJsonAsync<Equipment>(JsonOptions);
        Assert.NotNull(created);
        Assert.True(created!.Id > 0);

        var delete = await client.DeleteAsync($"/api/v1/equipment/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var audit = await client.GetFromJsonAsync<AuditLogDto[]>("/api/v1/audit?entityName=Equipment&take=10", JsonOptions);
        Assert.NotNull(audit);
        Assert.Contains(audit!, log => log.Action == "Create" && log.EntityId == created.Id && log.Succeeded);
        Assert.Contains(audit!, log => log.Action == "Delete" && log.EntityId == created.Id && log.Succeeded);
    }

    [Fact]
    public async Task Display_api_persists_definitions_and_viewer_loads_saved_display()
    {
        using var factory = new CoroMesWebFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        await SignInAsync(client);

        var displayName = $"Integration Display {Guid.NewGuid():N}"[..32];
        var create = await client.PostAsJsonAsync("/api/v1/displays", new
        {
            name = displayName,
            type = "equipment",
            refreshSeconds = 7,
            equipmentId = (int?)null,
            settingsJson = "{\"layout\":\"test\"}"
        });

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        var created = await create.Content.ReadFromJsonAsync<DisplayDto>(JsonOptions);
        Assert.NotNull(created);
        Assert.False(string.IsNullOrWhiteSpace(created!.Id));
        Assert.Equal(displayName, created.Name);
        Assert.Equal("equipment", created.Type);
        Assert.Equal(7, created.RefreshSeconds);

        var fetched = await client.GetFromJsonAsync<DisplayDto>($"/api/v1/displays/{created.Id}", JsonOptions);
        Assert.NotNull(fetched);
        Assert.Equal(created.Id, fetched!.Id);
        Assert.Equal(displayName, fetched.Name);

        var viewer = await client.GetAsync($"/displays/viewer?id={created.Id}");
        Assert.Equal(HttpStatusCode.OK, viewer.StatusCode);

        var audit = await client.GetFromJsonAsync<AuditLogDto[]>("/api/v1/audit?entityName=DisplayDefinition&take=10", JsonOptions);
        Assert.NotNull(audit);
        Assert.Contains(audit!, log => log.Action == "Create" && log.Succeeded);
    }

    [Fact]
    public async Task Upkeep_sync_writes_audit_record()
    {
        using var factory = new CoroMesWebFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        await SignInAsync(client);

        var assets = await client.GetFromJsonAsync<UpkeepAssetsDto>("/api/v1/integration/upkeep/assets", JsonOptions);
        Assert.NotNull(assets);
        Assert.Equal("mock", assets!.Mode);
        Assert.Contains(assets.Assets, asset => asset.Id == 101 && asset.Name == "Corrugator Main Drive");

        var sync = await client.PostAsync("/api/v1/integration/upkeep/sync", null);
        Assert.Equal(HttpStatusCode.OK, sync.StatusCode);

        var syncResult = await sync.Content.ReadFromJsonAsync<UpkeepSyncDto>(JsonOptions);
        Assert.NotNull(syncResult);
        Assert.True(syncResult!.Success);
        Assert.Equal("mock", syncResult.Mode);

        var audit = await client.GetFromJsonAsync<AuditLogDto[]>("/api/v1/audit?entityName=Upkeep&take=10", JsonOptions);
        Assert.NotNull(audit);
        Assert.Contains(audit!, log => log.Action == "Sync" && log.Succeeded);
    }

    [Fact]
    public async Task Alarm_lifecycle_persists_alert_summary_and_audit_records()
    {
        using var factory = new CoroMesWebFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        await SignInAsync(client);

        var create = await client.PostAsJsonAsync("/api/v1/alarms", new
        {
            title = "Integration test alarm",
            message = "Verifies mock dispatch and alarm lifecycle.",
            severity = AlarmSeverity.Critical,
            source = "integration-test",
            equipmentId = (int?)null,
            channels = new[] { "email", "sms", "pushover", "upkeep" }
        });

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        var created = await create.Content.ReadFromJsonAsync<AlarmDto>(JsonOptions);
        Assert.NotNull(created);
        Assert.True(created!.Id > 0);
        Assert.Equal("Critical", created.Severity);
        Assert.Equal("Active", created.Status);
        Assert.Equal("email,sms,pushover,upkeep", created.AlertChannels);
        Assert.Contains("email:ok", created.NotificationSummary);
        Assert.Contains("upkeep:ok", created.NotificationSummary);

        var fetched = await client.GetFromJsonAsync<AlarmDto>($"/api/v1/alarms/{created.Id}", JsonOptions);
        Assert.NotNull(fetched);
        Assert.Equal(created.Id, fetched!.Id);

        var alarms = await client.GetFromJsonAsync<AlarmDto[]>("/api/v1/alarms?status=Active&take=10", JsonOptions);
        Assert.NotNull(alarms);
        Assert.Contains(alarms!, alarm => alarm.Id == created.Id);

        var acknowledge = await client.PostAsync($"/api/v1/alarms/{created.Id}/acknowledge", null);
        Assert.Equal(HttpStatusCode.OK, acknowledge.StatusCode);
        var acknowledged = await acknowledge.Content.ReadFromJsonAsync<AlarmDto>(JsonOptions);
        Assert.Equal("Acknowledged", acknowledged?.Status);

        var resolve = await client.PostAsync($"/api/v1/alarms/{created.Id}/resolve", null);
        Assert.Equal(HttpStatusCode.OK, resolve.StatusCode);
        var resolved = await resolve.Content.ReadFromJsonAsync<AlarmDto>(JsonOptions);
        Assert.Equal("Resolved", resolved?.Status);

        var audit = await client.GetFromJsonAsync<AuditLogDto[]>("/api/v1/audit?entityName=AlarmEvent&take=10", JsonOptions);
        Assert.NotNull(audit);
        Assert.Contains(audit!, log => log.Action == "Create" && log.EntityId == created.Id && log.Succeeded);
        Assert.Contains(audit!, log => log.Action == "Acknowledge" && log.EntityId == created.Id && log.Succeeded);
        Assert.Contains(audit!, log => log.Action == "Resolve" && log.EntityId == created.Id && log.Succeeded);
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private static Task<HttpResponseMessage> SignInAsync(HttpClient client)
    {
        return client.PostAsync("/login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["accessCode"] = CoroMesWebFactory.AdminAccessCode,
            ["returnUrl"] = "/admin/equipment"
        }));
    }

    private sealed class AuditLogDto
    {
        public string Action { get; set; } = string.Empty;
        public int? EntityId { get; set; }
        public bool Succeeded { get; set; }
    }

    private sealed class DisplayDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public int RefreshSeconds { get; set; }
    }

    private sealed class UpkeepAssetsDto
    {
        public string Mode { get; set; } = string.Empty;
        public UpkeepAssetDto[] Assets { get; set; } = [];
    }

    private sealed class UpkeepAssetDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private sealed class UpkeepSyncDto
    {
        public bool Success { get; set; }
        public string Mode { get; set; } = string.Empty;
    }

    private sealed class AlarmDto
    {
        public int Id { get; set; }
        public string Severity { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? AlertChannels { get; set; }
        public string NotificationSummary { get; set; } = string.Empty;
    }

    private sealed class CoroMesWebFactory : WebApplicationFactory<Program>
    {
        public const string AdminAccessCode = "test-admin";

        private readonly string dbPath = Path.Combine(Path.GetTempPath(), $"coromes-web-tests-{Guid.NewGuid():N}.db");

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["DatabaseProvider"] = "sqlite",
                    ["ConnectionStrings:DefaultConnection"] = $"Data Source={dbPath}",
                    ["Auth:AdminAccessCode"] = AdminAccessCode,
                    ["i3x:enabled"] = "false",
                    ["Upkeep:Mode"] = "mock",
                    ["Alerting:Mode"] = "mock",
                    ["Alerting:CriticalChannels:0"] = "email",
                    ["Alerting:CriticalChannels:1"] = "sms",
                    ["Alerting:CriticalChannels:2"] = "pushover",
                    ["Alerting:CriticalChannels:3"] = "upkeep"
                });
            });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            TryDelete(dbPath);
            TryDelete($"{dbPath}-shm");
            TryDelete($"{dbPath}-wal");
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}
