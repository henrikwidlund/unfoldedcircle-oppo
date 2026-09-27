using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Channels;

using Oppo;

using UnfoldedCircle.OppoBluRay.Logging;

namespace UnfoldedCircle.OppoBluRay.Discovery;

/// <summary>
/// Actively probes for and listens to the legacy "OREMOTE" reply, which every
/// model answers, on a single shared socket.
///
/// This MUST be one socket - the OREMOTE probe is a broadcast, and the
/// player replies to the exact local port the probe was sent from. If the
/// probe is sent from a different socket than the one listening for replies,
/// the two sockets end up racing for port 7624 and which one actually
/// receives an incoming datagram becomes OS/platform dependent.
/// </summary>
public sealed class OppoUdpDiscovery(ILogger<OppoUdpDiscovery> logger) : IPlayerDiscovery, IDisposable
{
    private const int DiscoveryPort = 7624;

    private static readonly IPEndPoint BroadcastEndpoint = new(IPAddress.Broadcast, DiscoveryPort);
    private static readonly byte[] OremoteProbeMessage = [.. "NOTIFY OREMOTE LOGIN"u8];
    private const string OremoteMarker = "REPORT ADDRESS TO OREMOTE:";

    // Matches the Magnetar/SSDP rescan cadence elsewhere in this codebase.
    private static readonly TimeSpan OremoteProbeInterval = TimeSpan.FromMinutes(10);

    // Binds a fixed port (7624), refcounted by active DiscoverAsync callers:
    // bound on the first, shared by any others via a per-caller channel,
    // released once the last one ends. Avoids both rebinding per call (which
    // would race two sockets for the same port) and holding the bind for the
    // process's whole life (which would block other software from using it).
    private readonly Lock _startLock = new();
    private readonly ConcurrentDictionary<Guid, ChannelWriter<PlayerInfo>> _subscribers = new();
    private readonly ILogger<OppoUdpDiscovery> _logger = logger;
    private UdpClient? _socket;
    private CancellationTokenSource? _listenerCts;

    public string Name => "Oppo UDP-20x Broadcast + Legacy OREMOTE";

    public async IAsyncEnumerable<PlayerInfo> DiscoverAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var subscriberId = Guid.NewGuid();
        var channel = Channel.CreateUnbounded<PlayerInfo>();
        var socket = StartListeningAndRegister(subscriberId, channel.Writer);

        // The listener's own probe (below) only repeats every 10 minutes, so
        // without this, an attempt starting shortly after that scheduled
        // probe would just sit idle for up to 10 minutes with nothing to
        // trigger a reply. Every attempt gets its own immediate probe
        // instead, so every "Add device" click gets a fresh, near-instant
        // round trip.
        _ = SendProbeSafeAsync(socket, cancellationToken);

        try
        {
            while (await channel.Reader.WaitToReadAsync(cancellationToken))
            {
                while (channel.Reader.TryRead(out var player))
                    yield return player;
            }
        }
        finally
        {
            UnregisterAndStopIfIdle(subscriberId);
        }
    }

    private UdpClient StartListeningAndRegister(Guid subscriberId, ChannelWriter<PlayerInfo> writer)
    {
        lock (_startLock)
        {
            // Registering before checking/creating the socket, all under the
            // same lock as teardown. Teardown only runs when it observes zero subscribers,
            // and it can't observe that mid-way through this method adding one.
            _subscribers[subscriberId] = writer;

            if (_socket is not null)
                return _socket;

            var udp = CreateSocket();
            var cts = new CancellationTokenSource();
            _socket = udp;
            _listenerCts = cts;

            _ = RunProbeLoopAsync(udp, cts.Token);
            _ = RunListenLoopAsync(udp, cts.Token);

            return udp;
        }
    }

    private void UnregisterAndStopIfIdle(Guid subscriberId)
    {
        lock (_startLock)
        {
            _subscribers.TryRemove(subscriberId, out _);

            if (!_subscribers.IsEmpty)
                return; // another setup flow is still (or newly) using it

            _listenerCts?.Cancel();
            _listenerCts?.Dispose();
            _listenerCts = null;
            _socket?.Dispose();
            _socket = null;
        }
    }

    private async Task SendProbeSafeAsync(UdpClient udp, CancellationToken cancellationToken)
    {
        try
        {
            await SendProbeAsync(udp, cancellationToken);
        }
        catch (OperationCanceledException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
        catch (Exception ex)
        {
            _logger.OremoteProbeSendFailed(ex);
        }
    }

    private async Task RunListenLoopAsync(UdpClient udp, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            UdpReceiveResult result;

            try
            {
                result = await udp.ReceiveAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (ObjectDisposedException)
            {
                // Socket was torn down as part of shutdown.
                return;
            }

            // Gated on having an active subscriber: the socket itself is only
            // bound while at least one is registered (see
            // StartListeningAndRegister/UnregisterAndStopIfIdle), but this
            // guards against the brief window where a datagram is already
            // in flight right as the last subscriber tears things down -
            // nobody wants every OREMOTE reply/probe echo logged once a
            // setup flow is no longer waiting on discovery.
            if (!_subscribers.IsEmpty && _logger.IsEnabled(LogLevel.Trace))
                _logger.ReceivedDiscoveryDatagram(Encoding.ASCII.GetString(result.Buffer));

            PlayerInfo? player;
            try
            {
                player = ParsePacket(result.Buffer);
            }
            catch (Exception ex)
            {
                // A single malformed/unexpected datagram must not silently
                // kill this loop: nothing observes this fire-and-forget task,
                // so an unhandled exception here would stop the socket from
                // ever being read again for the rest of the process's life.
                _logger.FailedToParseDiscoveryDatagram(ex);
                continue;
            }

            if (player is null)
                continue;

            foreach (var writer in _subscribers.Values)
                writer.TryWrite(player);
        }
    }

    private static UdpClient CreateSocket()
    {
        var udp = new UdpClient(AddressFamily.InterNetwork);
        udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        // Needed so this same socket can also send the OREMOTE broadcast probe.
        udp.EnableBroadcast = true;
        udp.Client.Bind(new IPEndPoint(IPAddress.Any, DiscoveryPort));

        return udp;
    }

    public void Dispose()
    {
        lock (_startLock)
        {
            _listenerCts?.Cancel();
            _listenerCts?.Dispose();
            _listenerCts = null;
            _socket?.Dispose();
            _socket = null;
        }
    }

    private async Task RunProbeLoopAsync(UdpClient udp, CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await SendProbeAsync(udp, cancellationToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException and not ObjectDisposedException)
                {
                    // A single failed probe (e.g. a transient socket/network
                    // error) should not silently and permanently end
                    // discovery of legacy players for the rest of the
                    // process's life - log it and keep trying on schedule.
                    _logger.OremoteProbeSendFailed(ex);
                }

                await Task.Delay(OremoteProbeInterval, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown.
        }
        catch (ObjectDisposedException)
        {
            // Socket disposed out from under us during shutdown - fine.
        }
    }

    private static ValueTask<int> SendProbeAsync(UdpClient udp, CancellationToken cancellationToken)
        => udp.SendAsync(OremoteProbeMessage, BroadcastEndpoint, cancellationToken);

    private static PlayerInfo? ParsePacket(byte[] buffer)
    {
        var message = Encoding.ASCII.GetString(buffer);

        return ParseOremoteReply(message);
    }

    // Every model (pre-20x and 20x alike) answers OREMOTE with its model
    // designation as part of the reply text (e.g. "UDP-203_OPPO UDP-203
    // REPORT ADDRESS TO OREMOTE:..."), so a plain substring check against
    // the whole message is enough to identify it.
    private static readonly (string Token, OppoModel Model)[] ModelTokens =
    [
        ("UDP-203", OppoModel.UDP203),
        ("UDP-205", OppoModel.UDP205),
        ("BDP-83", OppoModel.BDP83),
        ("BDP-93", OppoModel.BDP9X),
        ("BDP-95", OppoModel.BDP9X),
        ("BDP-103", OppoModel.BDP10X),
        ("BDP-105", OppoModel.BDP10X),
    ];

    private static OppoModel? GuessModelFromOremoteText(string message)
    {
        foreach (var (token, model) in ModelTokens)
        {
            if (message.Contains(token, StringComparison.OrdinalIgnoreCase))
                return model;
        }

        return null;
    }

    private static PlayerInfo? ParseOremoteReply(string message)
    {
        var messageSpan = message.AsSpan().TrimEnd('\0');

        var markerIndex = messageSpan.IndexOf(OremoteMarker, StringComparison.Ordinal);

        if (markerIndex < 0)
            return null;

        /*
         * Example (BDP-10X, underscore-delimited):
         *
         * BDP-10X_My Oppo_REPORT ADDRESS TO OREMOTE:192.168.1.10:48360\0
         *
         * Format: <type>_<name>_REPORT ADDRESS TO OREMOTE:<ip>:<port>\0
         *
         * A real UDP-203 was observed replying with a plain space instead of
         * the second underscore, and a space after the colon too:
         *
         * UDP-203_OPPO UDP-203 REPORT ADDRESS TO OREMOTE: 192.168.1.10:19999
         *
         * so the separator right before the marker is trimmed as either
         * character, whichever is actually present.
         */

        // Trimming both separator chars in one pass matters here: a stray
        // trailing "NAME _" (space, then underscore) would only lose the
        // underscore if trimmed one char at a time in a fixed order, since
        // TrimEnd(' ') alone stops the moment it sees a non-space trailing
        // char - even if that char is the other separator this is meant to
        // strip too.
        var prefix = messageSpan[..markerIndex].TrimEnd(" _");

        var firstUnderscore = prefix.IndexOf('_');

        var playerName = (firstUnderscore >= 0 ? prefix[(firstUnderscore + 1)..] : prefix).Trim().ToString();

        var addressPart = messageSpan[(markerIndex + OremoteMarker.Length)..].Trim();

        var separator = addressPart.LastIndexOf(':');

        if (separator < 0)
            return null;

        var hostSpan = addressPart[..separator].Trim();
        var portText = addressPart[(separator + 1)..].Trim();

        if (!IPAddress.TryParse(hostSpan, out _))
            return null;

        if (!int.TryParse(portText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var port))
            return null;

        var host = hostSpan.ToString();

        if (string.IsNullOrWhiteSpace(playerName))
            playerName = host;

        return new PlayerInfo(
            DeviceId: host,
            Host: host,
            Port: port,
            DisplayName: playerName,
            Source: DiscoverySource.OppoLegacyOremote,
            Model: GuessModelFromOremoteText(message),
            FriendlyName: playerName);
    }
}
