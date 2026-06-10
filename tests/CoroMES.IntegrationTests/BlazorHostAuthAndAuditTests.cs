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

        var sync = await client.PostAsync("/api/v1/integration/upkeep/sync", null);
        Assert.Equal(HttpStatusCode.OK, sync.StatusCode);

        var audit = await client.GetFromJsonAsync<AuditLogDto[]>("/api/v1/audit?entityName=Upkeep&take=10", JsonOptions);
        Assert.NotNull(audit);
        Assert.Contains(audit!, log => log.Action == "Sync" && log.Succeeded);
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
                    ["i3x:enabled"] = "false"
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
