using CoroMES.Integration.Cti.Abstractions;
using CoroMES.Integration.Cti.Configuration;
using CoroMES.Integration.Cti.Models;
using Microsoft.Extensions.Options;

namespace CoroMES.Integration.Cti.Services;

public sealed class CtiParsedFileValidator : ICtiParsedFileValidator
{
    private readonly CtiConnectorOptions _options;

    public CtiParsedFileValidator(IOptions<CtiConnectorOptions> options)
    {
        _options = options.Value;
    }

    public IReadOnlyList<CtiValidationIssue> Validate(CtiParsedFile parsedFile)
    {
        var issues = new List<CtiValidationIssue>();
        var extension = Path.GetExtension(parsedFile.RawFile.OriginalFileName);
        var allowedExtensions = _options.DefaultExtensions
            .Select(NormalizeExtension)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (allowedExtensions.Count > 0 && !allowedExtensions.Contains(NormalizeExtension(extension)))
        {
            issues.Add(new CtiValidationIssue(
                CtiIssueSeverity.Warning,
                "CTI_FILE_EXTENSION_UNREGISTERED",
                $"File extension '{extension}' is not in the configured default CTI extension list.",
                parsedFile.RawFile.OriginalFileName));
        }

        if (parsedFile.Records.Count == 0)
        {
            issues.Add(new CtiValidationIssue(
                CtiIssueSeverity.Error,
                "CTI_FILE_EMPTY",
                "The CTI file contained no non-empty records.",
                parsedFile.RawFile.OriginalFileName));
        }

        foreach (var record in parsedFile.Records)
        {
            if (record.Fields.Count == 0)
            {
                issues.Add(new CtiValidationIssue(
                    CtiIssueSeverity.Error,
                    "CTI_RECORD_HAS_NO_FIELDS",
                    "The parsed record has no fields.",
                    parsedFile.RawFile.OriginalFileName,
                    record.LineNumber));
            }
        }

        return issues;
    }

    private static string NormalizeExtension(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
        {
            return string.Empty;
        }

        return extension.StartsWith(".", StringComparison.Ordinal)
            ? extension
            : $".{extension}";
    }
}
