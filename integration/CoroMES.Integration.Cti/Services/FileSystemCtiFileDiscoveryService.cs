using CoroMES.Integration.Cti.Abstractions;
using CoroMES.Integration.Cti.Configuration;
using CoroMES.Integration.Cti.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoroMES.Integration.Cti.Services;

public sealed class FileSystemCtiFileDiscoveryService : ICtiFileDiscoveryService
{
    private readonly CtiConnectorOptions _options;
    private readonly ILogger<FileSystemCtiFileDiscoveryService> _logger;

    public FileSystemCtiFileDiscoveryService(
        IOptions<CtiConnectorOptions> options,
        ILogger<FileSystemCtiFileDiscoveryService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public Task<IReadOnlyList<CtiFileCandidate>> DiscoverAsync(CancellationToken cancellationToken = default)
    {
        var candidates = new List<CtiFileCandidate>();

        foreach (var sourceFolder in _options.SourceFolders.Where(folder => !string.IsNullOrWhiteSpace(folder.Path)))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!Directory.Exists(sourceFolder.Path))
            {
                _logger.LogWarning(
                    "Configured CTI source folder {SourceName} does not exist: {SourcePath}",
                    sourceFolder.Name,
                    sourceFolder.Path);
                continue;
            }

            var allowedExtensions = GetAllowedExtensions(sourceFolder);

            foreach (var filePath in Directory.EnumerateFiles(sourceFolder.Path))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var fileInfo = new FileInfo(filePath);
                var extension = NormalizeExtension(fileInfo.Extension);

                if (allowedExtensions.Count > 0 && !allowedExtensions.Contains(extension))
                {
                    continue;
                }

                candidates.Add(new CtiFileCandidate(
                    sourceFolder.Name,
                    fileInfo.FullName,
                    fileInfo.Name,
                    extension,
                    fileInfo.Length,
                    fileInfo.CreationTimeUtc,
                    fileInfo.LastWriteTimeUtc));
            }
        }

        var ordered = candidates
            .OrderBy(candidate => candidate.LastWriteTimeUtc)
            .ThenBy(candidate => candidate.FileName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return Task.FromResult<IReadOnlyList<CtiFileCandidate>>(ordered);
    }

    private HashSet<string> GetAllowedExtensions(CtiSourceFolderOptions sourceFolder)
    {
        var extensions = sourceFolder.Extensions.Count > 0
            ? sourceFolder.Extensions
            : _options.DefaultExtensions;

        return extensions
            .Select(NormalizeExtension)
            .Where(extension => !string.IsNullOrWhiteSpace(extension))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
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
