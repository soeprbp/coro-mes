using CoroMES.Integration.Cti.Models;

namespace CoroMES.Integration.Cti.Abstractions;

public interface ICtiFileParser
{
    Task<CtiParsedFile> ParseAsync(CtiRawFile rawFile, CancellationToken cancellationToken = default);
}
