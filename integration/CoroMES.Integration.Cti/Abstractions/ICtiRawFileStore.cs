using CoroMES.Integration.Cti.Models;

namespace CoroMES.Integration.Cti.Abstractions;

public interface ICtiRawFileStore
{
    Task<CtiRawFile> CaptureAsync(CtiFileCandidate candidate, Guid correlationId, CancellationToken cancellationToken = default);

    Task<CtiQuarantineFile> QuarantineAsync(
        CtiFileCandidate candidate,
        Guid correlationId,
        string reason,
        CancellationToken cancellationToken = default);
}
