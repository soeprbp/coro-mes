using System.Text;
using CoroMES.Integration.Cti.Abstractions;
using CoroMES.Integration.Cti.Configuration;
using CoroMES.Integration.Cti.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoroMES.Integration.Cti.Parsing;

public sealed class ConservativeCtiFileParser : ICtiFileParser
{
    private static readonly char[] CandidateDelimiters = ['|', ',', '\t', ';', '~'];
    private readonly CtiConnectorOptions _options;
    private readonly ILogger<ConservativeCtiFileParser> _logger;

    public ConservativeCtiFileParser(
        IOptions<CtiConnectorOptions> options,
        ILogger<ConservativeCtiFileParser> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<CtiParsedFile> ParseAsync(CtiRawFile rawFile, CancellationToken cancellationToken = default)
    {
        var encoding = Encoding.GetEncoding(_options.TextEncoding);
        var lines = await File.ReadAllLinesAsync(rawFile.RawPath, encoding, cancellationToken);
        var delimiter = DetectDelimiter(lines);
        var records = new List<CtiParsedRecord>();
        var issues = new List<CtiValidationIssue>();

        for (var i = 0; i < lines.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var line = lines[i];
            var lineNumber = i + 1;

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var record = delimiter.HasValue
                ? ParseDelimitedLine(line, lineNumber, delimiter.Value)
                : ParseRawLine(line, lineNumber);

            records.Add(record);
        }

        if (!delimiter.HasValue)
        {
            issues.Add(new CtiValidationIssue(
                CtiIssueSeverity.Warning,
                "CTI_PARSER_NO_DELIMITER_DETECTED",
                "No common delimiter was detected. Records were preserved as raw lines until a real CTI layout is registered.",
                rawFile.OriginalFileName));
        }

        _logger.LogInformation(
            "Parsed CTI raw file {FileName} with {RecordCount} records and delimiter {Delimiter}",
            rawFile.OriginalFileName,
            records.Count,
            delimiter?.ToString() ?? "<none>");

        return new CtiParsedFile(rawFile, records, issues);
    }

    private static char? DetectDelimiter(IReadOnlyList<string> lines)
    {
        var sample = lines.FirstOrDefault(line => !string.IsNullOrWhiteSpace(line));
        if (sample is null)
        {
            return null;
        }

        return CandidateDelimiters
            .Select(delimiter => new { Delimiter = delimiter, Count = sample.Count(c => c == delimiter) })
            .Where(candidate => candidate.Count > 0)
            .OrderByDescending(candidate => candidate.Count)
            .Select(candidate => (char?)candidate.Delimiter)
            .FirstOrDefault();
    }

    private static CtiParsedRecord ParseDelimitedLine(string line, int lineNumber, char delimiter)
    {
        var values = line.Split(delimiter);
        var fields = values
            .Select((value, index) => new KeyValuePair<string, string>($"Field{index + 1:000}", value.Trim()))
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);

        var recordType = values.FirstOrDefault()?.Trim();
        if (string.IsNullOrWhiteSpace(recordType))
        {
            recordType = null;
        }

        return new CtiParsedRecord(lineNumber, line, recordType, fields);
    }

    private static CtiParsedRecord ParseRawLine(string line, int lineNumber)
    {
        var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Field001"] = line
        };

        return new CtiParsedRecord(lineNumber, line, null, fields);
    }
}
