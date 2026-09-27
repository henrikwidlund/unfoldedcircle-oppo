using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

using UnfoldedCircle.OppoBluRay.Logging;

namespace UnfoldedCircle.OppoBluRay.Discovery;

public sealed class DiscoveryService(IEnumerable<IPlayerDiscovery> discoveries, ILogger<DiscoveryService> logger)
{
    private readonly IEnumerable<IPlayerDiscovery> _discoveries = discoveries;
    private readonly ILogger<DiscoveryService> _logger = logger;

    public async IAsyncEnumerable<PlayerInfo> DiscoverAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var channel = Channel.CreateUnbounded<PlayerInfo>();

        var producerTasks = _discoveries
            .Select(d => RunDiscoveryAsync(
                d,
                channel.Writer,
                cancellationToken));

        // Once every producer has finished (normally, on error, or on
        // cancellation) complete the channel so the reader below doesn't
        // block forever waiting for an item that will never arrive.
        _ = CompleteWhenDoneAsync(producerTasks, channel.Writer);

        var seen = new ConcurrentDictionary<string, PlayerInfo>(StringComparer.OrdinalIgnoreCase);

        while (true)
        {
            bool hasItem;
            try
            {
                hasItem = await channel.Reader.WaitToReadAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                yield break;
            }

            if (!hasItem)
                yield break; // channel completed and drained: every producer is done

            if (!channel.Reader.TryRead(out var player))
                continue;

            var key = CreateKey(player);

            if (seen.TryAdd(key, player))
            {
                yield return player;
                continue;
            }

            var existing = seen[key];

            var merged = Merge(existing, player);

            // Only re-emit when the merge actually added something (record equality compares by value).
            if (merged == existing)
                continue;

            seen[key] = merged;
            yield return merged;
        }
    }

    private async Task RunDiscoveryAsync(IPlayerDiscovery discovery, ChannelWriter<PlayerInfo> writer, CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var player in discovery.DiscoverAsync(cancellationToken))
                await writer.WriteAsync(player, cancellationToken);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _logger.DiscoverySourceFailed(discovery.Name, ex);
        }
    }

    private static async Task CompleteWhenDoneAsync(IEnumerable<Task> producerTasks, ChannelWriter<PlayerInfo> writer)
    {
        try
        {
            await Task.WhenAll(producerTasks);
        }
        finally
        {
            // RunDiscoveryAsync already swallows every exception from its
            // discovery, so producerTasks never actually faults.
            writer.Complete();
        }
    }

    // Keyed on host (not DeviceId) so the same physical player is recognized
    // as one device across every discovery source, even though some sources
    // (SsdpDiscovery) populate DeviceId from the UPnP USN rather than the host.
    private static string CreateKey(PlayerInfo player) => player.Host.Trim().ToLowerInvariant();

    private static PlayerInfo Merge(PlayerInfo existing, PlayerInfo incoming) =>
        existing with
        {
            Model = existing.Model ?? incoming.Model,
            Manufacturer = existing.Manufacturer ?? incoming.Manufacturer,
            ModelName = existing.ModelName ?? incoming.ModelName,
            FriendlyName = existing.FriendlyName ?? incoming.FriendlyName,
            Location = existing.Location ?? incoming.Location,
            Usn = existing.Usn ?? incoming.Usn,
            DisplayName = !string.IsNullOrWhiteSpace(existing.DisplayName)
                ? existing.DisplayName
                : incoming.DisplayName
        };
}
