using CoroMES.Core.Entities;
using CoroMES.Infrastructure.Data;
using CoroMES.Web.Services;
using Microsoft.EntityFrameworkCore;

namespace CoroMES.Web.Endpoints;

public static class DisplayEndpoints
{
    public static RouteGroupBuilder MapDisplayEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/displays", async (ApplicationDbContext db) =>
        {
            var displays = await db.DisplayDefinitions
                .AsNoTracking()
                .Where(display => display.IsActive)
                .OrderBy(display => display.Name)
                .Select(display => DisplayResponse.FromEntity(display))
                .ToListAsync();

            return Results.Ok(displays);
        })
        .WithName("GetDisplays")
        .WithTags("Displays");

        api.MapGet("/displays/{id}", async (string id, ApplicationDbContext db) =>
        {
            var display = await db.DisplayDefinitions
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Slug == id && item.IsActive);

            return display is null ? Results.NotFound() : Results.Ok(DisplayResponse.FromEntity(display));
        })
        .WithName("GetDisplay")
        .WithTags("Displays");

        api.MapPost("/displays", async (DisplayRequest request, ApplicationDbContext db, IAuditLogService auditLog, HttpContext context) =>
        {
            var display = new DisplayDefinition
            {
                Slug = await CreateUniqueSlugAsync(db, CleanName(request.Name)),
                Name = CleanName(request.Name),
                Type = CleanType(request.Type),
                RefreshSeconds = Math.Max(1, request.RefreshSeconds),
                EquipmentId = request.EquipmentId,
                SettingsJson = request.SettingsJson,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            db.DisplayDefinitions.Add(display);
            await db.SaveChangesAsync();
            await auditLog.RecordAsync(context, new AuditLogEntry(
                "Create",
                nameof(DisplayDefinition),
                display.Id,
                DescribeDisplay(display)));

            return Results.Created($"/api/v1/displays/{display.Slug}", DisplayResponse.FromEntity(display));
        })
        .WithName("CreateDisplay")
        .WithTags("Displays");

        api.MapPut("/displays/{id}", async (string id, DisplayRequest request, ApplicationDbContext db, IAuditLogService auditLog, HttpContext context) =>
        {
            var display = await db.DisplayDefinitions.FirstOrDefaultAsync(item => item.Slug == id && item.IsActive);
            if (display is null)
            {
                await auditLog.RecordAsync(context, new AuditLogEntry(
                    "Update",
                    nameof(DisplayDefinition),
                    null,
                    $"Display update failed because slug {id} was not found.",
                    Succeeded: false));
                return Results.NotFound();
            }

            var before = DescribeDisplay(display);
            display.Name = CleanName(request.Name);
            display.Type = CleanType(request.Type);
            display.RefreshSeconds = Math.Max(1, request.RefreshSeconds);
            display.EquipmentId = request.EquipmentId;
            display.SettingsJson = request.SettingsJson;
            display.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();
            await auditLog.RecordAsync(context, new AuditLogEntry(
                "Update",
                nameof(DisplayDefinition),
                display.Id,
                $"{before} -> {DescribeDisplay(display)}"));
            return Results.Ok(DisplayResponse.FromEntity(display));
        })
        .WithName("UpdateDisplay")
        .WithTags("Displays");

        return api;
    }

    private static async Task<string> CreateUniqueSlugAsync(ApplicationDbContext db, string name)
    {
        var root = Slugify(CleanName(name));
        var slug = root;
        var suffix = 1;

        while (await db.DisplayDefinitions.AnyAsync(display => display.Slug == slug))
        {
            suffix++;
            slug = $"{root}-{suffix}";
        }

        return slug;
    }

    private static string CleanName(string? name)
    {
        return string.IsNullOrWhiteSpace(name) ? "New Display" : name.Trim();
    }

    private static string CleanType(string? type)
    {
        var value = string.IsNullOrWhiteSpace(type) ? "oee" : type.Trim().ToLowerInvariant();
        return value is "oee" or "production" or "quality" or "equipment" ? value : "oee";
    }

    private static string Slugify(string value)
    {
        var chars = value
            .ToLowerInvariant()
            .Select(ch => char.IsLetterOrDigit(ch) ? ch : '-')
            .ToArray();

        var slug = string.Join('-', new string(chars).Split('-', StringSplitOptions.RemoveEmptyEntries));
        return string.IsNullOrWhiteSpace(slug) ? $"display-{DateTime.UtcNow:HHmmss}" : slug;
    }

    private static string DescribeDisplay(DisplayDefinition display)
    {
        return $"Slug={display.Slug}; Name={display.Name}; Type={display.Type}; RefreshSeconds={display.RefreshSeconds}; EquipmentId={display.EquipmentId?.ToString() ?? "none"}";
    }

    public sealed record DisplayRequest(string? Name, string? Type, int RefreshSeconds, int? EquipmentId, string? SettingsJson);

    public sealed record DisplayResponse(string Id, string Name, string Type, int RefreshSeconds, int? EquipmentId, string? SettingsJson)
    {
        public static DisplayResponse FromEntity(DisplayDefinition display)
        {
            return new DisplayResponse(
                display.Slug,
                display.Name,
                display.Type,
                display.RefreshSeconds,
                display.EquipmentId,
                display.SettingsJson);
        }
    }
}
