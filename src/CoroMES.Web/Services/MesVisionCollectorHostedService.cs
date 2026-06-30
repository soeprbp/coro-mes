using CoroMES.Integration.IIoT.Configuration;

namespace CoroMES.Web.Services;

public sealed class MesVisionCollectorHostedService(
    IServiceScopeFactory scopeFactory,
    MesVisionCollectorOptions options,
    ILogger<MesVisionCollectorHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Enabled)
        {
            logger.LogInformation("MES-Vision collector background polling is disabled.");
            return;
        }

        var interval = TimeSpan.FromSeconds(Math.Max(10, options.PollIntervalSeconds));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var runner = scope.ServiceProvider.GetRequiredService<IMesVisionCollectorRunner>();
                await runner.CollectOnceAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "MES-Vision collector background poll failed.");
            }

            await Task.Delay(interval, stoppingToken);
        }
    }
}
