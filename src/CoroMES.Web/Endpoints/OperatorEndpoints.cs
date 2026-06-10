using CoroMES.Core.Entities;
using CoroMES.Core.Interfaces.Repositories;

namespace CoroMES.Web.Endpoints;

public static class OperatorEndpoints
{
    public static RouteGroupBuilder MapOperatorEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/operators", async (IOperatorRepository operatorRepo) =>
        {
            var operators = await operatorRepo.GetAllAsync();
            return Results.Ok(operators.Take(100));
        })
        .WithName("GetOperators")
        .WithTags("Operators");

        api.MapGet("/operators/{id}", async (int id, IOperatorRepository operatorRepo) =>
        {
            var op = await operatorRepo.GetByIdAsync(id);
            return op is null ? Results.NotFound() : Results.Ok(op);
        })
        .WithName("GetOperator")
        .WithTags("Operators");

        api.MapPost("/operators", async (Operator op, IOperatorRepository operatorRepo) =>
        {
            op.CreatedAt = DateTime.UtcNow;
            await operatorRepo.AddAsync(op);
            return Results.Created($"/api/v1/operators/{op.Id}", op);
        })
        .WithName("CreateOperator")
        .WithTags("Operators");

        return api;
    }
}
