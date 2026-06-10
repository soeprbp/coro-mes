using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CoroMES.Integration.Alerts;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAlertingIntegration(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection("Alerting").Get<AlertingOptions>() ?? new AlertingOptions();
        services.AddSingleton(options);

        var mode = NormalizeMode(options.Mode);
        if (mode == "disabled")
        {
            services.AddSingleton<IAlertDispatcher, DisabledAlertDispatcher>();
        }
        else if (mode == "live")
        {
            services.AddSingleton<IAlertDispatcher, GuardedLiveAlertDispatcher>();
        }
        else
        {
            services.AddSingleton<IAlertDispatcher, MockAlertDispatcher>();
        }

        return services;
    }

    private static string NormalizeMode(string? mode)
    {
        return string.IsNullOrWhiteSpace(mode) ? "mock" : mode.Trim().ToLowerInvariant();
    }
}
