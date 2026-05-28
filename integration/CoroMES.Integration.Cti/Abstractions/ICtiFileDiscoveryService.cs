using CoroMES.Integration.Cti.Models;

namespace CoroMES.Integration.Cti.Abstractions;

public interface ICtiFileDiscoveryService
{
    Task<IReadOnlyList<CtiFileCandidate>> DiscoverAsync(CancellationToken cancellationToken = default);
}
