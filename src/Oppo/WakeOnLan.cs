using System.Net;
using System.Net.Sockets;

namespace Oppo;

internal static class WakeOnLan
{
    private static readonly IPAddress BroadcastAddress = IPAddress.Parse("255.255.255.255");

    public static async ValueTask SendWakeOnLanAsync(string macAddress)
    {
        byte[] magicPacket = CreateMagicPacket(macAddress);
        using var socket = new Socket(BroadcastAddress.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
        socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
        await socket.SendToAsync(magicPacket, new IPEndPoint(BroadcastAddress, 9));
    }

    private static byte[] CreateMagicPacket(string macAddress) =>
        Convert.FromHexString(new string('F', 12)
                              + string.Concat(Enumerable.Repeat(
                                  macAddress.Replace(":", string.Empty, StringComparison.Ordinal).Replace("-", string.Empty, StringComparison.Ordinal),
                                  16
                              )));
}