using System.Net;
using System.Net.Sockets;

namespace Oppo;

internal static class WakeOnLan
{
    private static readonly IPEndPoint BroadcastIpEndPoint = new(IPAddress.Parse("255.255.255.255"), 9);

    public static async ValueTask SendWakeOnLanAsync(string macAddress)
    {
        byte[] magicPacket = CreateMagicPacket(macAddress);
        using var socket = new Socket(BroadcastIpEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
        socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
        await socket.SendToAsync(magicPacket, BroadcastIpEndPoint);
    }

    private static byte[] CreateMagicPacket(string macAddress) =>
        Convert.FromHexString(new string('F', 12)
                              + string.Concat(Enumerable.Repeat(
                                  macAddress.Replace(":", string.Empty, StringComparison.Ordinal).Replace("-", string.Empty, StringComparison.Ordinal),
                                  16
                              )));
}