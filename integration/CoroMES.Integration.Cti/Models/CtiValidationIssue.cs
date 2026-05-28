namespace CoroMES.Integration.Cti.Models;

public sealed record CtiValidationIssue(
    CtiIssueSeverity Severity,
    string Code,
    string Message,
    string? FileName = null,
    int? LineNumber = null);

public enum CtiIssueSeverity
{
    Info,
    Warning,
    Error
}
