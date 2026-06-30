using CoroMES.Integration.IIoT.Models;

namespace CoroMES.Integration.IIoT.Abstractions;

public interface IMesVisionCollectorClient
{
    Task<MesVisionCollectionSnapshot> CollectAsync(CancellationToken cancellationToken = default);
}
