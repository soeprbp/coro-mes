using System.Security.Cryptography;
using CoroMES.Integration.Cti.Configuration;
using CoroMES.Integration.Cti.Models;
using CoroMES.Integration.Cti.Parsing;
using CoroMES.Integration.Cti.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace CoroMES.UnitTests;

public sealed class CtiConnectorTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "coromes-cti-tests", Guid.NewGuid().ToString("N"));

    public CtiConnectorTests()
    {
        Directory.CreateDirectory(_root);
    }

    [Fact]
    public async Task FileDiscovery_ReturnsOnlyConfiguredCtiExtensions()
    {
        var inbound = Path.Combine(_root, "inbound");
        Directory.CreateDirectory(inbound);
        await File.WriteAllTextAsync(Path.Combine(inbound, "job.dat"), "JOB|WO123");
        await File.WriteAllTextAsync(Path.Combine(inbound, "job.cov"), "COV|WO123");
        await File.WriteAllTextAsync(Path.Combine(inbound, "ignore.txt"), "ignore");

        var service = new FileSystemCtiFileDiscoveryService(
            Options.Create(new CtiConnectorOptions
            {
                SourceFolders =
                [
                    new CtiSourceFolderOptions
                    {
                        Name = "cti-bridge",
                        Path = inbound
                    }
                ]
            }),
            NullLogger<FileSystemCtiFileDiscoveryService>.Instance);

        var files = await service.DiscoverAsync();

        Assert.Equal(2, files.Count);
        Assert.All(files, file => Assert.Equal("cti-bridge", file.SourceName));
        Assert.Contains(files, file => file.FileName == "job.dat");
        Assert.Contains(files, file => file.FileName == "job.cov");
        Assert.DoesNotContain(files, file => file.FileName == "ignore.txt");
    }

    [Fact]
    public async Task RawFileStore_CapturesSourceWithoutMutatingOriginal()
    {
        var inbound = Path.Combine(_root, "inbound");
        Directory.CreateDirectory(inbound);
        var sourcePath = Path.Combine(inbound, "production.dat");
        await File.WriteAllTextAsync(sourcePath, "PROD|WO123|100");

        var candidate = CreateCandidate("cti-production", sourcePath);
        var store = new FileSystemCtiRawFileStore(
            Options.Create(new CtiConnectorOptions
            {
                RawLandingRoot = Path.Combine(_root, "raw"),
                QuarantineRoot = Path.Combine(_root, "quarantine")
            }),
            NullLogger<FileSystemCtiRawFileStore>.Instance);

        var rawFile = await store.CaptureAsync(candidate, Guid.NewGuid());

        Assert.True(File.Exists(sourcePath));
        Assert.True(File.Exists(rawFile.RawPath));
        Assert.Equal(await ComputeSha256Async(sourcePath), rawFile.Sha256);
        Assert.Equal(await File.ReadAllTextAsync(sourcePath), await File.ReadAllTextAsync(rawFile.RawPath));
    }

    [Fact]
    public async Task ConservativeParser_PreservesDelimitedFields()
    {
        var rawPath = Path.Combine(_root, "schedule.dat");
        await File.WriteAllLinesAsync(rawPath, ["JOB|WO123|LINE1", "PROD|WO123|25"]);
        var rawFile = new CtiRawFile(
            "cti-bridge",
            rawPath,
            "schedule.dat",
            rawPath,
            await ComputeSha256Async(rawPath),
            new FileInfo(rawPath).Length,
            DateTimeOffset.UtcNow,
            Guid.NewGuid());

        var parser = new ConservativeCtiFileParser(
            Options.Create(new CtiConnectorOptions()),
            NullLogger<ConservativeCtiFileParser>.Instance);

        var parsed = await parser.ParseAsync(rawFile);

        Assert.Equal(2, parsed.Records.Count);
        Assert.Empty(parsed.Issues);
        Assert.Equal("JOB", parsed.Records[0].RecordType);
        Assert.Equal("WO123", parsed.Records[0].Fields["Field002"]);
        Assert.Equal("LINE1", parsed.Records[0].Fields["Field003"]);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private static CtiFileCandidate CreateCandidate(string sourceName, string path)
    {
        var fileInfo = new FileInfo(path);
        return new CtiFileCandidate(
            sourceName,
            fileInfo.FullName,
            fileInfo.Name,
            fileInfo.Extension,
            fileInfo.Length,
            fileInfo.CreationTimeUtc,
            fileInfo.LastWriteTimeUtc);
    }

    private static async Task<string> ComputeSha256Async(string path)
    {
        await using var stream = File.OpenRead(path);
        var hash = await SHA256.HashDataAsync(stream);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
