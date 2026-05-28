namespace CoroMES.Integration.Cti.Configuration;

public sealed class CtiConnectorOptions
{
    public bool Enabled { get; set; }
    public string RawLandingRoot { get; set; } = "data/cti/raw";
    public string QuarantineRoot { get; set; } = "data/cti/quarantine";
    public string ArchiveRoot { get; set; } = "data/cti/archive";
    public string TextEncoding { get; set; } = "utf-8";
    public List<string> DefaultExtensions { get; set; } = [".dat", ".cov"];
    public List<CtiSourceFolderOptions> SourceFolders { get; set; } = [];
}

public sealed class CtiSourceFolderOptions
{
    public string Name { get; set; } = "default";
    public string Path { get; set; } = string.Empty;
    public List<string> Extensions { get; set; } = [];
}
