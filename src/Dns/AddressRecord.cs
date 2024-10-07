using System.Net;
using System.Net.Sockets;

namespace Makaretu.Dns;

/// <summary>
///   Base class for an Internet address.
/// </summary>
public abstract class AddressRecord : ResourceRecord
{
    /// <summary>
    ///   The Internet address.
    /// </summary>
    /// <value>
    ///   Either IPv4 or IPv6.
    /// </value>
    public IPAddress? Address { get; set; }

    /// <summary>
    ///   Creates an A or AAAA record based on the <see cref="AddressFamily"/>.
    /// </summary>
    /// <param name="name">
    ///   The name of the node that owns the address.
    /// </param>
    /// <param name="address">
    ///   An IPv4 or IPv6 address.
    /// </param>
    /// <returns>
    ///   An <see cref="ARecord"/> or <see cref="AAAARecord"/> tha describes
    ///   the <paramref name="name"/> and <paramref name="address"/>.
    /// </returns>
    public static AddressRecord Create(DomainName name, IPAddress address) =>
        address.AddressFamily switch
        {
            AddressFamily.InterNetwork => new ARecord { Name = name, Address = address },
            _ => new AAAARecord { Name = name, Address = address }
        };

    /// <inheritdoc />
    public override void ReadData(WireReader reader, int length) => Address = reader.ReadIPAddress(length);

    /// <inheritdoc />
    public override void ReadData(PresentationReader reader) => Address = reader.ReadIPAddress();

    /// <inheritdoc />
    public override void WriteData(WireWriter writer)
    {
        ArgumentNullException.ThrowIfNull(Address);
        writer.WriteIPAddress(Address);
    }

    /// <inheritdoc />
    public override void WriteData(PresentationWriter writer)
    {
        ArgumentNullException.ThrowIfNull(Address);
        writer.WriteIPAddress(Address, appendSpace: false);
    }
}