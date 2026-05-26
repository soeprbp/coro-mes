using System.Net.Http.Json;
using CoroMES.Industrial.i3X.Configuration;
using CoroMES.Industrial.i3X.Models;
using CoroMES.Industrial.i3X.Translators;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoroMES.Industrial.i3X;

/// <summary>
/// Extension methods for registering i3X services in DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds i3X client services to the application.
    /// </summary>
    public static IServiceCollection AddI3X(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<I3XOptions>(configuration.GetSection("i3x"));
        services.AddSingleton<IMesEntityTranslator, MesEntityTranslator>();

        services.AddHttpClient<II3XClient, I3XClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<I3XOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);

            if (!string.IsNullOrWhiteSpace(options.ApiKey) && !options.ApiKey.StartsWith("${", StringComparison.Ordinal))
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {options.ApiKey}");
            }
        });

        // Note: Object type registration is currently manual due to admin requirements.
        // Call I3XClient methods to register object types if needed.
        // Alternatively, implement a separate initialization service.

        return services;
    }
}
