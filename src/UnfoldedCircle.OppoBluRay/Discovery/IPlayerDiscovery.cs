namespace UnfoldedCircle.OppoBluRay.Discovery;

public interface IPlayerDiscovery
{
    string Name { get; }

    IAsyncEnumerable<PlayerInfo> DiscoverAsync(CancellationToken cancellationToken = default);
}
