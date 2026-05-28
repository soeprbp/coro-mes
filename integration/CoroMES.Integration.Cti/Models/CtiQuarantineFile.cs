namespace CoroMES.Integration.Cti.Models;

public sealed record CtiQuarantineFile(
    string SourceName,
    string OriginalPath,
    string QuarantinePath,
    string ReasonPath,
    string Reason,
    DateTimeOffset QuarantinedAtUtc,
    Guid CorrelationId);
