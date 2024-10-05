namespace Oppo;

public interface IOppoClientFactory
{
    IOppoClient? TryGetOrCreateClient(in OppoClientKey oppoClientKey);
    void TryDisposeClient(in OppoClientKey oppoClientKey);
    public void TryDisposeAllClients();
}