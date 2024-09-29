using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;

namespace Makaretu.Dns;

/// <summary>
///   The event data for <see cref="MulticastService.NetworkInterfaceDiscovered"/>.
/// </summary>
public class NetworkInterfaceEventArgs : EventArgs
{
    /// <summary>
    ///   The sequence of detected network interfaces.
    /// </summary>
    /// <value>
    ///   A sequence of network interfaces.
    /// </value>
    public IEnumerable<NetworkInterface>? NetworkInterfaces { get; init; }
}