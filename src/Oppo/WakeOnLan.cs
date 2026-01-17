using System.Net;
using System.Net.Sockets;

namespace Oppo;

internal static class WakeOnLan
{
    private static readonly IPEndPoint BroadcastIpEndPointPort7 = new(IPAddress.Parse("255.255.255.255"), 7);
    private static readonly IPEndPoint BroadcastIpEndPointPort9 = new(IPAddress.Parse("255.255.255.255"), 9);

    public static async ValueTask SendWakeOnLanAsync(string macAddress, IPAddress ipAddress)
    {
        byte[] magicPacket = CreateMagicPacket(macAddress);
        using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
        var broadcastAddress = ipAddress.ToString();
        var lastDotIndex = broadcastAddress.LastIndexOf('.');
        broadcastAddress = $"{broadcastAddress[..(lastDotIndex + 1)]}255";
        var broadcastIp = IPAddress.Parse(broadcastAddress);

        await Task.WhenAll(
            socket.SendToAsync(magicPacket, BroadcastIpEndPointPort7),
            socket.SendToAsync(magicPacket, BroadcastIpEndPointPort9),
            socket.SendToAsync(magicPacket, new IPEndPoint(broadcastIp, 7)),
            socket.SendToAsync(magicPacket, new IPEndPoint(broadcastIp, 9)));
    }

    private static byte[] CreateMagicPacket(string macAddress) =>
        Convert.FromHexString(new string('F', 12)
                              + string.Concat(Enumerable.Repeat(
                                  macAddress.Replace(":", string.Empty, StringComparison.Ordinal).Replace("-", string.Empty, StringComparison.Ordinal),
                                  16
                              )));
}