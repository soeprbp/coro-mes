using CoroMES.Integration.Cti.Abstractions;
using CoroMES.Integration.Cti.Models;
using Microsoft.Extensions.Logging;

namespace CoroMES.Integration.Cti.Services;

public sealed class CtiFileIngestionPipeline : ICtiIngestionPipeline
{
    private readonly ICtiFileDiscoveryService _discoveryService;
    private readonly ICtiRawFileStore _rawFileStore;
    private readonly ICtiFileParser _parser;
    private readonly ICtiParsedFileValidator _validator;
    private readonly ILogger<CtiFileIngestionPipeline> _logger;

    public CtiFileIngestionPipeline(
        ICtiFileDiscoveryService discoveryService,
        ICtiRawFileStore rawFileStore,
        ICtiFileParser parser,
        ICtiParsedFileValidator validator,
        ILogger<CtiFileIngestionPipeline> logger)
    {
        _discoveryService = discoveryService;
        _rawFileStore = rawFileStore;
        _parser = parser;
        _validator = validator;
        _logger = logger;
    }

    public async Task<CtiIngestionResult> IngestAsync(CancellationToken cancellationToken = default)
    {
        var correlationId = Guid.NewGuid();
        var startedAt = DateTimeOffset.UtcNow;
        var issues = new List<CtiValidationIssue>();
        var ingestedFiles = 0;
        var quarantinedFiles = 0;

        var candidates = await _discoveryService.DiscoverAsync(cancellationToken);

        foreach (var candidate in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var rawFile = await _rawFileStore.CaptureAsync(candidate, correlationId, cancellationToken);
                var parsedFile = await _parser.ParseAsync(rawFile, cancellationToken);
                var fileIssues = parsedFile.Issues.Concat(_validator.Validate(parsedFile)).ToList();

                issues.AddRange(fileIssues);

                if (fileIssues.Any(issue => issue.Severity == CtiIssueSeverity.Error))
                {
                    quarantinedFiles++;
                    await _rawFileStore.QuarantineAsync(
                        candidate,
                        correlationId,
                        "Validation failed. See ingestion result issues for details.",
                        cancellationToken);
                    continue;
                }

                ingestedFiles++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                quarantinedFiles++;
                issues.Add(new CtiValidationIssue(
                    CtiIssueSeverity.Error,
                    "CTI_INGESTION_FILE_FAILED",
                    ex.Message,
                    candidate.FileName));

                await _rawFileStore.QuarantineAsync(candidate, correlationId, ex.Message, cancellationToken);
            }
        }

        var completedAt = DateTimeOffset.UtcNow;
        _logger.LogInformation(
            "Completed CTI ingestion {CorrelationId}: discovered={DiscoveredFiles}, ingested={IngestedFiles}, quarantined={QuarantinedFiles}",
            correlationId,
            candidates.Count,
            ingestedFiles,
            quarantinedFiles);

        return new CtiIngestionResult(
            correlationId,
            startedAt,
            completedAt,
            candidates.Count,
            ingestedFiles,
            quarantinedFiles,
            issues);
    }
}
