using System.Net;
using System.Net.Sockets;

namespace Oppo;

internal static class WakeOnLan
{
    public static async ValueTask SendWakeOnLanAsync(IPAddress ipAddress, string macAddress)
    {
        byte[] magicPacket = CreateMagicPacket(macAddress);
        using var socket = new Socket(ipAddress.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
        socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
        await socket.ConnectAsync(ipAddress, 9);
        await socket.SendAsync(magicPacket);
    }

    private static byte[] CreateMagicPacket(string macAddress) =>
        Convert.FromHexString(new string('F', 12)
                              + string.Concat(Enumerable.Repeat(
                                  macAddress.Replace(":", string.Empty, StringComparison.Ordinal).Replace("-", string.Empty, StringComparison.Ordinal),
                                  16
                              )));
}