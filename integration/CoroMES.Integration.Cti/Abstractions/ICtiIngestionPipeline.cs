using CoroMES.Integration.Cti.Models;

namespace CoroMES.Integration.Cti.Abstractions;

public interface ICtiIngestionPipeline
{
    Task<CtiIngestionResult> IngestAsync(CancellationToken cancellationToken = default);
}
