namespace Makaretu.Dns;

/// <summary>
///   Mail exchange.
/// </summary>
/// <remarks>
///   MX records cause type A additional section processing for the host
///   specified by EXCHANGE.The use of MX RRs is explained in detail in
///   [RFC-974].
/// </remarks>
public class MXRecord : ResourceRecord
{
    /// <summary>
    ///   Creates a new instance of the <see cref="MXRecord"/> class.
    /// </summary>
    public MXRecord() => Type = DnsType.MX;

    /// <summary>
    ///  The preference given to this RR among others at the same owner. 
    /// </summary>
    /// <value>
    ///   Lower values are preferred.
    /// </value>
    public ushort? Preference { get; set; }

    /// <summary>
    ///  A domain-name which specifies a host willing to act as
    ///  a mail exchange for the owner name.
    /// </summary>
    /// <value>
    ///   The name of an mail exchange.
    /// </value>
    public DomainName? Exchange { get; set; }


    /// <inheritdoc />
    public override void ReadData(WireReader reader, int length)
    {
        Preference = reader.ReadUInt16();
        Exchange = reader.ReadDomainName();
    }

    /// <inheritdoc />
    public override void ReadData(PresentationReader reader)
    {
        Preference = reader.ReadUInt16();
        Exchange = reader.ReadDomainName();
    }

    /// <inheritdoc />
    public override void WriteData(WireWriter writer)
    {
        if (Preference is null)
            throw new InvalidOperationException("Preference is required.");
        
        writer.WriteUInt16(Preference.Value);
        writer.WriteDomainName(Exchange);
    }

    /// <inheritdoc />
    public override void WriteData(PresentationWriter writer)
    {
        if (Preference is null)
            throw new InvalidOperationException("Preference is required.");
        
        writer.WriteUInt16(Preference.Value);
        writer.WriteDomainName(Exchange, appendSpace: false);
    }
}