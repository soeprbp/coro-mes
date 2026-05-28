namespace CoroMES.Integration.Cti.Models;

public sealed record CtiRawFile(
    string SourceName,
    string OriginalPath,
    string OriginalFileName,
    string RawPath,
    string Sha256,
    long Length,
    DateTimeOffset CapturedAtUtc,
    Guid CorrelationId);
