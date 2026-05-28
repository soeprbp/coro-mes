namespace CoroMES.Integration.Cti.Models;

public sealed record CtiFileCandidate(
    string SourceName,
    string FullPath,
    string FileName,
    string Extension,
    long Length,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset LastWriteTimeUtc);
