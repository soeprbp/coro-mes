using CoroMES.Integration.Cti.Abstractions;
using CoroMES.Integration.Cti.Configuration;
using CoroMES.Integration.Cti.Parsing;
using CoroMES.Integration.Cti.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CoroMES.Integration.Cti;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCtiIntegration(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<CtiConnectorOptions>(configuration.GetSection("cti"));

        services.AddScoped<ICtiFileDiscoveryService, FileSystemCtiFileDiscoveryService>();
        services.AddScoped<ICtiRawFileStore, FileSystemCtiRawFileStore>();
        services.AddScoped<ICtiFileParser, ConservativeCtiFileParser>();
        services.AddScoped<ICtiParsedFileValidator, CtiParsedFileValidator>();
        services.AddScoped<ICtiIngestionPipeline, CtiFileIngestionPipeline>();

        return services;
    }
}
