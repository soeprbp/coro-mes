namespace CoroMES.Web.Endpoints;

public static class CompatibilityEndpoints
{
    public static IEndpointRouteBuilder MapCompatibilityEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/admin/index.html", () => Results.Redirect("/admin", permanent: false));
        app.MapGet("/displays/builder.html", () => Results.Redirect("/displays/builder", permanent: false));
        app.MapGet("/displays/viewer.html", (HttpContext context) =>
        {
            var query = context.Request.QueryString.HasValue ? context.Request.QueryString.Value : string.Empty;
            return Results.Redirect($"/displays/viewer{query}", permanent: false);
        });

        app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
            .WithName("Health Check");

        return app;
    }
}
