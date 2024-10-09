namespace Makaretu.Dns;

/// <summary>
///   Extended DNS Errors
/// </summary>
/// <remarks>
///  <para>
///  Defined in <see href="https://tools.ietf.org/html/rfc8914">RFC 8914 - Extended DNS Errors</see>
///  </para>
/// </remarks>
public class EdnsErrorOption : EdnsOption
{
    /// <summary>
    ///   Creates a new instance of the <see cref="EdnsErrorOption"/> class.
    /// </summary>
    public EdnsErrorOption()
    {
        Type = EdnsOptionType.ExtendedDNSError;
        Error = ExtendedDNSError.NotSupported;
        Text = string.Empty;
    }

    /// <summary>
    ///   The error number
    /// </summary>
    public ExtendedDNSError Error { get; set; }

    /// <summary>
    /// The error descriptive text
    /// </summary>
    public string Text { get; set; }

    /// <inheritdoc />
    public override void ReadData(WireReader reader, int length)
    {
        Error = (ExtendedDNSError)reader.ReadUInt16();
        Text = reader.ReadUTF8String(length - 2);
    }

    /// <inheritdoc />
    public override void WriteData(WireWriter writer)
    {
        writer.WriteUInt16((ushort)Error);
        writer.WriteStringUTF8Unprefixed(Text);
    }

    /// <inheritdoc />
    public override string ToString() => $";   Error = {Error} ({Text})";
}