namespace Oppo;

public interface IOppoClientFactory
{
    ValueTask<IOppoClient?> TryGetOrCreateClient(OppoClientKey oppoClientKey, CancellationToken cancellationToken);
    void TryDisposeClient(in OppoClientKey oppoClientKey);
    public void TryDisposeAllClients();
}