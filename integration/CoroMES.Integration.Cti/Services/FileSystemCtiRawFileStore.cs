using System.Security.Cryptography;
using System.Text;
using CoroMES.Integration.Cti.Abstractions;
using CoroMES.Integration.Cti.Configuration;
using CoroMES.Integration.Cti.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoroMES.Integration.Cti.Services;

public sealed class FileSystemCtiRawFileStore : ICtiRawFileStore
{
    private readonly CtiConnectorOptions _options;
    private readonly ILogger<FileSystemCtiRawFileStore> _logger;

    public FileSystemCtiRawFileStore(
        IOptions<CtiConnectorOptions> options,
        ILogger<FileSystemCtiRawFileStore> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<CtiRawFile> CaptureAsync(
        CtiFileCandidate candidate,
        Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        var capturedAt = DateTimeOffset.UtcNow;
        var sha256 = await ComputeSha256Async(candidate.FullPath, cancellationToken);
        var destinationDirectory = BuildDestinationDirectory(_options.RawLandingRoot, candidate.SourceName, capturedAt, correlationId);
        Directory.CreateDirectory(destinationDirectory);

        var destinationPath = BuildUniqueDestinationPath(
            destinationDirectory,
            $"{sha256[..12]}_{SanitizeFileName(candidate.FileName)}");

        await CopyFileAsync(candidate.FullPath, destinationPath, cancellationToken);

        _logger.LogInformation(
            "Captured CTI file {FileName} to raw landing {RawPath} with hash {Sha256}",
            candidate.FileName,
            destinationPath,
            sha256);

        return new CtiRawFile(
            candidate.SourceName,
            candidate.FullPath,
            candidate.FileName,
            destinationPath,
            sha256,
            candidate.Length,
            capturedAt,
            correlationId);
    }

    public async Task<CtiQuarantineFile> QuarantineAsync(
        CtiFileCandidate candidate,
        Guid correlationId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var quarantinedAt = DateTimeOffset.UtcNow;
        var destinationDirectory = BuildDestinationDirectory(_options.QuarantineRoot, candidate.SourceName, quarantinedAt, correlationId);
        Directory.CreateDirectory(destinationDirectory);

        var quarantinePath = BuildUniqueDestinationPath(destinationDirectory, SanitizeFileName(candidate.FileName));
        var reasonPath = $"{quarantinePath}.reason.txt";

        await CopyFileAsync(candidate.FullPath, quarantinePath, cancellationToken);
        await File.WriteAllTextAsync(reasonPath, reason, Encoding.UTF8, cancellationToken);

        _logger.LogWarning(
            "Quarantined CTI file {FileName} to {QuarantinePath}. Reason: {Reason}",
            candidate.FileName,
            quarantinePath,
            reason);

        return new CtiQuarantineFile(
            candidate.SourceName,
            candidate.FullPath,
            quarantinePath,
            reasonPath,
            reason,
            quarantinedAt,
            correlationId);
    }

    private static string BuildDestinationDirectory(
        string root,
        string sourceName,
        DateTimeOffset timestamp,
        Guid correlationId)
    {
        return Path.Combine(
            root,
            timestamp.UtcDateTime.ToString("yyyy"),
            timestamp.UtcDateTime.ToString("MM"),
            timestamp.UtcDateTime.ToString("dd"),
            SanitizeFileName(sourceName),
            correlationId.ToString("N"));
    }

    private static string BuildUniqueDestinationPath(string directory, string fileName)
    {
        var path = Path.Combine(directory, fileName);
        if (!File.Exists(path))
        {
            return path;
        }

        var name = Path.GetFileNameWithoutExtension(fileName);
        var extension = Path.GetExtension(fileName);
        return Path.Combine(directory, $"{name}_{Guid.NewGuid():N}{extension}");
    }

    private static async Task CopyFileAsync(string sourcePath, string destinationPath, CancellationToken cancellationToken)
    {
        await using var source = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        await using var destination = new FileStream(destinationPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await source.CopyToAsync(destination, cancellationToken);
    }

    private static async Task<string> ComputeSha256Async(string path, CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string SanitizeFileName(string value)
    {
        var invalidCharacters = Path.GetInvalidFileNameChars().ToHashSet();
        var sanitized = new string(value.Select(character => invalidCharacters.Contains(character) ? '_' : character).ToArray());
        return string.IsNullOrWhiteSpace(sanitized) ? "cti" : sanitized;
    }
}
