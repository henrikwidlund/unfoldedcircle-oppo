namespace Makaretu.Dns;

/// <summary>
///   Parameters needed by authoritative servers to calculate hashed owner names.
/// </summary>
/// <remarks>
///   Defined by <see href="https://tools.ietf.org/html/rfc5155#section-4">RFC 5155 - DNS Security (DNSSEC) Hashed Authenticated Denial of Existence</see>.
/// </remarks>
public class NSEC3PARAMRecord : ResourceRecord
{
    /// <summary>
    ///   Creates a new instance of the <see cref="NSEC3PARAMRecord"/> class.
    /// </summary>
    public NSEC3PARAMRecord() => Type = DnsType.NSEC3PARAM;

    /// <summary>
    ///   The cryptographic hash algorithm used to create the hashed owner name.
    /// </summary>
    /// <value>
    ///   One of the <see cref="DigestType"/> value.
    /// </value>
    public DigestType? HashAlgorithm { get; set; }

    /// <summary>
    ///   Not used, must be zero.
    /// </summary>
    public byte? Flags { get; set; }

    /// <summary>
    ///   Number of times to perform the <see cref="HashAlgorithm"/>.
    /// </summary>
    public ushort? Iterations { get; set; }

    /// <summary>
    ///   Appended to the original owner name before hashing.
    /// </summary>
    /// <remarks>
    ///   Used to defend against pre-calculated dictionary attacks.
    /// </remarks>
    public byte[]? Salt { get; set; }

    /// <inheritdoc />
    public override void ReadData(WireReader reader, int length)
    {
        HashAlgorithm = (DigestType)reader.ReadByte();
        Flags = reader.ReadByte();
        Iterations = reader.ReadUInt16();
        Salt = reader.ReadByteLengthPrefixedBytes();
    }

    /// <inheritdoc />
    public override void WriteData(WireWriter writer)
    {
        if (HashAlgorithm is null)
            throw new InvalidOperationException("HashAlgorithm is required.");
        
        if (Flags is null)
            throw new InvalidOperationException("Flags is required.");
        
        if (Iterations is null)
            throw new InvalidOperationException("Iterations is required.");
        
        writer.WriteByte((byte)HashAlgorithm);
        writer.WriteByte(Flags.Value);
        writer.WriteUInt16(Iterations.Value);
        writer.WriteByteLengthPrefixedBytes(Salt);
    }

    /// <inheritdoc />
    public override void ReadData(PresentationReader reader)
    {
        HashAlgorithm = (DigestType)reader.ReadByte();
        Flags = reader.ReadByte();
        Iterations = reader.ReadUInt16();

        var salt = reader.ReadString();
        if (!salt.Equals("-", StringComparison.Ordinal))
            Salt = BaseConvert.FromBase16(salt);
    }

    /// <inheritdoc />
    public override void WriteData(PresentationWriter writer)
    {
        if (HashAlgorithm is null)
            throw new InvalidOperationException("HashAlgorithm is required.");
        
        if (Flags is null)
            throw new InvalidOperationException("Flags is required.");
        
        if (Iterations is null)
            throw new InvalidOperationException("Iterations is required.");
        
        writer.WriteByte((byte)HashAlgorithm);
        writer.WriteByte((byte)Flags);
        writer.WriteUInt16(Iterations.Value);

        if (Salt == null || Salt.Length == 0)
        {
            writer.WriteString("-");
        }
        else
        {
            writer.WriteBase16String(Salt, appendSpace: false);
        }
    }
}