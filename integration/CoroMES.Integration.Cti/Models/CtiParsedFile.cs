namespace CoroMES.Integration.Cti.Models;

public sealed record CtiParsedFile(
    CtiRawFile RawFile,
    IReadOnlyList<CtiParsedRecord> Records,
    IReadOnlyList<CtiValidationIssue> Issues);

public sealed record CtiParsedRecord(
    int LineNumber,
    string RawLine,
    string? RecordType,
    IReadOnlyDictionary<string, string> Fields);
