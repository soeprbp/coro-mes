namespace CoroMES.Web.Endpoints;

public static class ApiEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapCoroMesApiEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api/v1").RequireAuthorization("AdminOnly");

        api.MapAuditEndpoints();
        api.MapAlarmEndpoints();
        api.MapSettingsEndpoints();
        api.MapWorkOrderEndpoints();
        api.MapEquipmentEndpoints();
        api.MapUpkeepEndpoints();
        api.MapMesVisionEndpoints();
        api.MapMaterialEndpoints();
        api.MapOperatorEndpoints();
        api.MapQualityEndpoints();
        api.MapDisplayEndpoints();

        return app;
    }
}
