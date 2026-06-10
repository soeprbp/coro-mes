using CoroMES.Core.Entities;
using CoroMES.Core.Interfaces.Repositories;

namespace CoroMES.Web.Endpoints;

public static class WorkOrderEndpoints
{
    public static RouteGroupBuilder MapWorkOrderEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/workorders", async (IWorkOrderRepository workOrderRepo) =>
        {
            var workOrders = await workOrderRepo.GetAllAsync();
            return Results.Ok(workOrders.Take(100));
        })
        .WithName("GetWorkOrders")
        .WithTags("WorkOrders");

        api.MapGet("/workorders/{id}", async (int id, IWorkOrderRepository workOrderRepo) =>
        {
            var workOrder = await workOrderRepo.GetByIdAsync(id);
            return workOrder is null ? Results.NotFound() : Results.Ok(workOrder);
        })
        .WithName("GetWorkOrder")
        .WithTags("WorkOrders");

        api.MapPost("/workorders", async (WorkOrder workOrder, IWorkOrderRepository workOrderRepo) =>
        {
            workOrder.CreatedAt = DateTime.UtcNow;
            workOrder.Number = $"WO-{DateTime.UtcNow:yyyyMMddHHmmss}";
            await workOrderRepo.AddAsync(workOrder);
            return Results.Created($"/api/v1/workorders/{workOrder.Id}", workOrder);
        })
        .WithName("CreateWorkOrder")
        .WithTags("WorkOrders");

        return api;
    }
}
