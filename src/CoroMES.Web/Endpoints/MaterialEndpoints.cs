using CoroMES.Core.Entities;
using CoroMES.Core.Interfaces.Repositories;

namespace CoroMES.Web.Endpoints;

public static class MaterialEndpoints
{
    public static RouteGroupBuilder MapMaterialEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/materials", async (IMaterialRepository materialRepo) =>
        {
            var materials = await materialRepo.GetAllAsync();
            return Results.Ok(materials.Take(100));
        })
        .WithName("GetMaterials")
        .WithTags("Materials");

        api.MapGet("/materials/{id}", async (int id, IMaterialRepository materialRepo) =>
        {
            var material = await materialRepo.GetByIdAsync(id);
            return material is null ? Results.NotFound() : Results.Ok(material);
        })
        .WithName("GetMaterial")
        .WithTags("Materials");

        api.MapPost("/materials", async (Material material, IMaterialRepository materialRepo) =>
        {
            material.CreatedAt = DateTime.UtcNow;
            await materialRepo.AddAsync(material);
            return Results.Created($"/api/v1/materials/{material.Id}", material);
        })
        .WithName("CreateMaterial")
        .WithTags("Materials");

        return api;
    }
}
