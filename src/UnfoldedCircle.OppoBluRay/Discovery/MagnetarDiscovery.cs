using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;

using Oppo;

using UnfoldedCircle.OppoBluRay.Logging;

namespace UnfoldedCircle.OppoBluRay.Discovery;

/// <summary>
/// Actively probes for Magnetar players via an SSDP-style M-SEARCH burst.
/// Magnetar players never self-announce - they only reply to this probe -
/// and their reply isn't a real SSDP device description, just a payload
/// containing the token "MAGNETAR". Unlike <see cref="SsdpDiscovery"/>
/// this identifies the model with certainty, but still can't learn the
/// MAC address needed for Wake-on-LAN this way.
/// </summary>
public sealed class MagnetarDiscovery(ILogger<MagnetarDiscovery> logger) : IPlayerDiscovery
{
    private readonly ILogger<MagnetarDiscovery> _logger = logger;

    private const string SsdpAddress = "239.255.255.250";
    private const int SsdpPort = 1900;
    private const string BroadcastAddress = "255.255.255.255";
    private const int MagnetarControlPort = 8102;
    private const string MagnetarToken = "MAGNETAR";

    private const int BurstCount = 3;
    private static readonly TimeSpan BurstInterval = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan ListenWindow = TimeSpan.FromSeconds(5);

    private static readonly byte[] SearchMessage =
        Encoding.ASCII.GetBytes(
            "M-SEARCH * HTTP/1.1\r\n" +
            "HOST: 239.255.255.250:1900\r\n" +
            "MAN: \"ssdp:discover\"\r\n" +
            "MX: 3\r\n" +
            "ST: ssdp:all\r\n" +
            "\r\n");

    public string Name => "Magnetar SSDP Probe";

    public async IAsyncEnumerable<PlayerInfo> DiscoverAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var udp = new UdpClient();
        udp.EnableBroadcast = true;

        using var timeout = new CancellationTokenSource(ListenWindow);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);

        // Fire-and-forget: shares `udp` with the receive loop below on purpose.
        _ = SendBurstAsync(udp, linked.Token);

        var seenHosts = new HashSet<string>(StringComparer.Ordinal);

        while (!linked.IsCancellationRequested)
        {
            UdpReceiveResult result;
            try
            {
                result = await udp.ReceiveAsync(linked.Token);
            }
            catch (OperationCanceledException)
            {
                yield break;
            }
            catch (ObjectDisposedException)
            {
                yield break;
            }

            var text = Encoding.ASCII.GetString(result.Buffer);
            if (!text.Contains(MagnetarToken, StringComparison.OrdinalIgnoreCase))
                continue;

            var host = result.RemoteEndPoint.Address.ToString();
            if (!seenHosts.Add(host))
                continue;

            yield return new PlayerInfo(
                DeviceId: host,
                Host: host,
                Port: MagnetarControlPort,
                DisplayName: "Magnetar",
                Source: DiscoverySource.Magnetar,
                Model: OppoModel.Magnetar,
                FriendlyName: "Magnetar");
        }
    }

    private async Task SendBurstAsync(UdpClient udp, CancellationToken cancellationToken)
    {
        try
        {
            for (var i = 0; i < BurstCount; i++)
            {
                await udp.SendAsync(SearchMessage, SearchMessage.Length, SsdpAddress, SsdpPort);
                await udp.SendAsync(SearchMessage, SearchMessage.Length, BroadcastAddress, SsdpPort);
                await Task.Delay(BurstInterval, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown once the listen window elapses.
        }
        catch (ObjectDisposedException)
        {
            // Socket disposed during shutdown.
        }
        catch (Exception ex)
        {
            // A failed burst just means fewer chances to be found this round
            _logger.MagnetarBurstFailed(ex);
        }
    }
}
