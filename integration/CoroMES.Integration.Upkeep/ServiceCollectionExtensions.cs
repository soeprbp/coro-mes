using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CoroMES.Integration.Upkeep;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUpkeepIntegration(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection("Upkeep").Get<UpkeepOptions>() ?? new UpkeepOptions();
        services.AddSingleton(options);

        var mode = NormalizeMode(options.Mode);
        if (mode == "live")
        {
            services.AddHttpClient<IUpkeepIntegration, LiveUpkeepIntegration>(client =>
            {
                if (!string.IsNullOrWhiteSpace(options.BaseUrl))
                {
                    client.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);
                }

                client.Timeout = TimeSpan.FromSeconds(Math.Max(1, options.TimeoutSeconds));
            });
        }
        else if (mode == "disabled")
        {
            services.AddSingleton<IUpkeepIntegration, DisabledUpkeepIntegration>();
        }
        else
        {
            services.AddSingleton<IUpkeepIntegration, MockUpkeepIntegration>();
        }

        return services;
    }

    private static string NormalizeMode(string? mode)
    {
        return string.IsNullOrWhiteSpace(mode) ? "mock" : mode.Trim().ToLowerInvariant();
    }
}
