using CoroMES.Integration.IIoT.Abstractions;
using CoroMES.Integration.IIoT.Configuration;
using CoroMES.Integration.IIoT.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CoroMES.Integration.IIoT;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMesVisionCollector(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection("MesVisionCollector").Get<MesVisionCollectorOptions>() ?? new MesVisionCollectorOptions();
        services.AddSingleton(options);
        services.AddSingleton<MesVisionTelemetryNormalizer>();

        services.AddHttpClient<IMesVisionCollectorClient, MesVisionI3XCollectorClient>(client =>
        {
            client.BaseAddress = NormalizeBaseUri(options.I3XBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(Math.Max(1, options.TimeoutSeconds));

            if (!string.IsNullOrWhiteSpace(options.ApiKey))
            {
                client.DefaultRequestHeaders.Add("x-api-key", options.ApiKey);
            }
        });

        return services;
    }

    private static Uri NormalizeBaseUri(string baseUrl)
    {
        var normalized = string.IsNullOrWhiteSpace(baseUrl)
            ? "https://rocktumbler.57446516.xyz/i3x/v1/"
            : baseUrl.Trim();

        if (!normalized.EndsWith("/", StringComparison.Ordinal))
        {
            normalized += "/";
        }

        return new Uri(normalized, UriKind.Absolute);
    }
}
