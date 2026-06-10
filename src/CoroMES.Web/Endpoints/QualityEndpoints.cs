using CoroMES.Core.Interfaces.Repositories;

namespace CoroMES.Web.Endpoints;

public static class QualityEndpoints
{
    public static RouteGroupBuilder MapQualityEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/quality/inspections", async (IInspectionRepository inspectionRepo) =>
        {
            var inspections = await inspectionRepo.GetAllAsync();
            return Results.Ok(inspections.Take(100));
        })
        .WithName("GetInspections")
        .WithTags("Quality");

        api.MapGet("/quality/ncr", async (INonConformanceRepository ncrRepo) =>
        {
            var ncrs = await ncrRepo.GetAllAsync();
            return Results.Ok(ncrs.Take(100));
        })
        .WithName("GetNonConformances")
        .WithTags("Quality");

        return api;
    }
}
