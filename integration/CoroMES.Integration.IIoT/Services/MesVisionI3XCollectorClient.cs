using CoroMES.Industrial.i3X;
using CoroMES.Integration.IIoT.Abstractions;
using CoroMES.Integration.IIoT.Configuration;
using CoroMES.Integration.IIoT.Models;
using Microsoft.Extensions.Logging;

namespace CoroMES.Integration.IIoT.Services;

public sealed class MesVisionI3XCollectorClient(
    HttpClient httpClient,
    MesVisionCollectorOptions options,
    MesVisionTelemetryNormalizer normalizer,
    ILogger<MesVisionI3XCollectorClient> logger) : IMesVisionCollectorClient
{
    public async Task<MesVisionCollectionSnapshot> CollectAsync(CancellationToken cancellationToken = default)
    {
        var client = new I3XClient(httpClient);
        var collectedAt = DateTime.UtcNow;
        var historyEnd = collectedAt;
        var historyStart = historyEnd.AddMinutes(-Math.Max(1, options.HistoryLookbackMinutes));

        var info = await client.GetServerInfoAsync(cancellationToken);
        var objects = await client.GetObjectsAsync(includeMetadata: true, cancellationToken: cancellationToken);
        var elementIds = objects
            .Select(item => item.ElementId)
            .Where(elementId => !string.IsNullOrWhiteSpace(elementId))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var values = elementIds.Count > 0
            ? await client.GetObjectValuesAsync(elementIds, Math.Max(1, options.MaxDepth), cancellationToken)
            : new();

        var history = elementIds.Contains("events", StringComparer.OrdinalIgnoreCase)
            ? await client.GetObjectHistoryAsync(["events"], historyStart, historyEnd, Math.Max(1, options.MaxDepth), cancellationToken)
            : new();

        logger.LogInformation(
            "Collected MES-Vision i3X snapshot from {BaseUrl}: objects={ObjectCount}, values={ValueCount}, historyKeys={HistoryKeyCount}",
            httpClient.BaseAddress,
            objects.Count,
            values.Count,
            history.Count);

        return normalizer.Normalize(new MesVisionNormalizationInput(
            options.ExternalSystemId,
            options.DisplayName,
            options.I3XBaseUrl,
            options.DashboardBaseUrl,
            collectedAt,
            info,
            objects,
            values,
            history));
    }
}
