using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

namespace Makaretu.Dns;

/// <summary>
///   Performs the magic to send and receive datagrams over multicast
///   sockets.
/// </summary>
internal class MulticastClient : IDisposable
{
    private readonly ILogger<MulticastClient>? _logger;

    /// <summary>
    ///   The port number assigned to Multicast DNS.
    /// </summary>
    /// <value>
    ///   Port number 5353.
    /// </value>
    public const int MulticastPort = 5353;

    static readonly IPAddress MulticastAddressIp4 = IPAddress.Parse("224.0.0.251");
    static readonly IPAddress MulticastAddressIp6 = IPAddress.Parse("FF02::FB");
    static readonly IPEndPoint MdnsEndpointIp6 = new(MulticastAddressIp6, MulticastPort);
    static readonly IPEndPoint MdnsEndpointIp4 = new(MulticastAddressIp4, MulticastPort);

    private readonly List<UdpClient> _receivers;
    private readonly ConcurrentDictionary<IPAddress, UdpClient> _senders = new();

    public Func<UdpReceiveResult, Task>? MessageReceived { get; set; }

    public MulticastClient(bool useIPv4, bool useIpv6, IEnumerable<NetworkInterface> nics, CancellationToken cancellationToken, ILogger<MulticastClient>? logger = null)
    {
        _logger = logger;
        // Set up the receivers.
        _receivers = [];

        UdpClient? receiver4 = null;
        if (useIPv4)
        {
            receiver4 = new UdpClient(AddressFamily.InterNetwork);
            receiver4.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            receiver4.Client.Bind(new IPEndPoint(IPAddress.Any, MulticastPort));
            _receivers.Add(receiver4);
        }

        UdpClient? receiver6 = null;
        if (useIpv6)
        {
            receiver6 = new UdpClient(AddressFamily.InterNetworkV6);
            receiver6.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            receiver6.Client.Bind(new IPEndPoint(IPAddress.IPv6Any, MulticastPort));
            _receivers.Add(receiver6);
        }

        // Get the IP addresses that we should send to.
        var addreses = nics
            .SelectMany(GetNetworkInterfaceLocalAddresses)
            .Where(a => (useIPv4 && a.AddressFamily == AddressFamily.InterNetwork)
                        || (useIpv6 && a.AddressFamily == AddressFamily.InterNetworkV6));
        foreach (var address in addreses)
        {
            if (_senders.ContainsKey(address))
            {
                continue;
            }

            var localEndpoint = new IPEndPoint(address, MulticastPort);
            var sender = new UdpClient(address.AddressFamily);
            try
            {
                switch (address.AddressFamily)
                {
                    case AddressFamily.InterNetwork:
                        MulticastOption mcastOption4 = new(MulticastAddressIp4, address);
                        receiver4?.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, mcastOption4);
                        sender.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                        sender.Client.Bind(localEndpoint);
                        sender.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, mcastOption4);
                        break;
                    case AddressFamily.InterNetworkV6:
                        IPv6MulticastOption mcastOption6 = new(MulticastAddressIp6, address.ScopeId);
                        receiver6?.Client.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.AddMembership, mcastOption6);
                        sender.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                        sender.Client.Bind(localEndpoint);
                        sender.Client.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.AddMembership, mcastOption6);
                        break;
                    default:
                        throw new NotSupportedException($"Address family {address.AddressFamily}.");
                }

                _receivers.Add(sender);
                _logger?.LogDebug("Will send via {localEndpoint}", localEndpoint);
                if (!_senders.TryAdd(address, sender)) // Should not fail
                {
                    sender.Dispose();
                }
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.AddressNotAvailable)
            {
                // VPN NetworkInterfaces
                sender.Dispose();
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Cannot setup send socket for {Address}", address);
                sender.Dispose();
            }
        }

        // Start listening for messages.
        foreach (var r in _receivers)
        {
            _ = Listen(r, cancellationToken);
        }
    }

    public async Task SendAsync(byte[] message)
    {
        foreach (var sender in _senders)
        {
            try
            {
                var endpoint = sender.Key.AddressFamily == AddressFamily.InterNetwork ? MdnsEndpointIp4 : MdnsEndpointIp6;
                await sender.Value.SendAsync(
                    message, message.Length,
                    endpoint);
            }
            catch (Exception e)
            {
                _logger?.LogInformation(e, "Sender {Key} failure.", sender.Key);
                // eat it.
            }
        }
    }
    
    private async Task Listen(UdpClient receiver, CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var result = await receiver.ReceiveAsync(cancellationToken);

                if (MessageReceived is not null)
                {
                    await MessageReceived(result).ConfigureAwait(false);
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Receiver failure.");
        }
    }
    
    private static IEnumerable<IPAddress> GetNetworkInterfaceLocalAddresses(NetworkInterface nic)
    {
        return nic
                .GetIPProperties()
                .UnicastAddresses
                .Select(static x => x.Address)
                .Where(static x => x.AddressFamily != AddressFamily.InterNetworkV6 || x.IsIPv6LinkLocal)
            ;
    }

    #region IDisposable Support

    private bool _disposedValue; // To detect redundant calls

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                MessageReceived = null;

                foreach (var receiver in _receivers)
                {
                    try
                    {
                        receiver.Dispose();
                    }
                    catch
                    {
                        // eat it.
                    }
                }
                _receivers.Clear();
                _senders.Clear();
            }

            _disposedValue = true;
        }
    }

    // This code added to correctly implement the disposable pattern.
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    #endregion
}