using CoroMES.Core.Entities;
using CoroMES.Core.Interfaces.Repositories;
using CoroMES.Web.Services;

namespace CoroMES.Web.Endpoints;

public static class EquipmentEndpoints
{
    public static RouteGroupBuilder MapEquipmentEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/equipment", async (IEquipmentRepository equipmentRepo) =>
        {
            var equipment = await equipmentRepo.GetAllAsync();
            return Results.Ok(equipment.Take(100));
        })
        .WithName("GetEquipment")
        .WithTags("Equipment");

        api.MapGet("/equipment/{id}", async (int id, IEquipmentRepository equipmentRepo) =>
        {
            var equipment = await equipmentRepo.GetByIdAsync(id);
            return equipment is null ? Results.NotFound() : Results.Ok(equipment);
        })
        .WithName("GetEquipmentById")
        .WithTags("Equipment");

        api.MapPost("/equipment", async (Equipment equipment, IEquipmentRepository equipmentRepo, IAuditLogService auditLog, HttpContext context) =>
        {
            equipment.CreatedAt = DateTime.UtcNow;
            await equipmentRepo.AddAsync(equipment);
            await auditLog.RecordAsync(context, new AuditLogEntry(
                "Create",
                nameof(Equipment),
                equipment.Id,
                DescribeEquipment(equipment)));
            return Results.Created($"/api/v1/equipment/{equipment.Id}", equipment);
        })
        .WithName("CreateEquipment")
        .WithTags("Equipment");

        api.MapPut("/equipment/{id}", async (int id, Equipment equipment, IEquipmentRepository equipmentRepo, IAuditLogService auditLog, HttpContext context) =>
        {
            var existing = await equipmentRepo.GetByIdAsync(id);
            if (existing is null)
            {
                await auditLog.RecordAsync(context, new AuditLogEntry(
                    "Update",
                    nameof(Equipment),
                    id,
                    $"Equipment update failed because id {id} was not found.",
                    Succeeded: false));
                return Results.NotFound();
            }

            var before = DescribeEquipment(existing);
            existing.Name = equipment.Name;
            existing.Code = equipment.Code;
            existing.Type = equipment.Type;
            existing.Location = equipment.Location;
            existing.IpAddress = equipment.IpAddress;
            existing.UpkeepAssetId = equipment.UpkeepAssetId;
            existing.PartsPerMinute = equipment.PartsPerMinute;
            existing.SqFtPerDay = equipment.SqFtPerDay;
            existing.CycleTimeSeconds = equipment.CycleTimeSeconds;
            existing.Protocol = equipment.Protocol;
            existing.Status = equipment.Status;
            existing.UpdatedAt = DateTime.UtcNow;

            await equipmentRepo.UpdateAsync(existing);
            await auditLog.RecordAsync(context, new AuditLogEntry(
                "Update",
                nameof(Equipment),
                existing.Id,
                $"{before} -> {DescribeEquipment(existing)}"));
            return Results.Ok(existing);
        })
        .WithName("UpdateEquipment")
        .WithTags("Equipment");

        api.MapDelete("/equipment/{id}", async (int id, IEquipmentRepository equipmentRepo, IAuditLogService auditLog, HttpContext context) =>
        {
            var equipment = await equipmentRepo.GetByIdAsync(id);
            if (equipment is null)
            {
                await auditLog.RecordAsync(context, new AuditLogEntry(
                    "Delete",
                    nameof(Equipment),
                    id,
                    $"Equipment delete failed because id {id} was not found.",
                    Succeeded: false));
                return Results.NotFound();
            }

            var summary = DescribeEquipment(equipment);
            await equipmentRepo.DeleteAsync(id);
            await auditLog.RecordAsync(context, new AuditLogEntry(
                "Delete",
                nameof(Equipment),
                id,
                summary));
            return Results.NoContent();
        })
        .WithName("DeleteEquipment")
        .WithTags("Equipment");

        return api;
    }

    private static string DescribeEquipment(Equipment equipment)
    {
        return $"Code={equipment.Code}; Name={equipment.Name}; Status={equipment.Status}; Location={equipment.Location ?? "n/a"}; UpKeepAssetId={equipment.UpkeepAssetId?.ToString() ?? "none"}";
    }
}
