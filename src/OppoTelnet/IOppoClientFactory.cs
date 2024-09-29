namespace OppoTelnet;

public interface IOppoClientFactory
{
    IOppoClient? TryGetOrCreateClient(OppoClientKey oppoClientKey);
    void TryDisposeClient(OppoClientKey oppoClientKey);
    public void TryDisposeAllClients();
}