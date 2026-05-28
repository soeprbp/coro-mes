namespace CoroMES.Integration.Cti.Models;

public sealed record CtiIngestionResult(
    Guid CorrelationId,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset CompletedAtUtc,
    int DiscoveredFiles,
    int IngestedFiles,
    int QuarantinedFiles,
    IReadOnlyList<CtiValidationIssue> Issues);
