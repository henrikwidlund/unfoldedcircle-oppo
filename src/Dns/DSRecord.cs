using System.Text;

namespace Makaretu.Dns;

/// <summary>
///   Delegation Signer.
/// </summary>
/// <remarks>
///   Defined in <see href="https://tools.ietf.org/html/rfc4034#section-5">RFC 4034 section 5</see>.
/// </remarks>
public class DSRecord : ResourceRecord
{
    /// <summary>
    ///   Creates a new instance of the <see cref="DSRecord"/> class.
    /// </summary>
    public DSRecord() => Type = DnsType.DS;

    /// <summary>
    ///   Creates a new instance of the <see cref="DSRecord"/> class
    ///   from the specified <see cref="DNSKEYRecord"/>.
    /// </summary>
    /// <param name="key">
    ///   The dns key to use.
    /// </param>
    /// <param name="force">
    ///   If <b>true</b>, key usage checks are ignored.
    /// </param>
    /// <exception cref="ArgumentException">
    ///   Both <see cref="DnsKeys.ZoneKey"/> and <see cref="DnsKeys.SecureEntryPoint"/>
    ///   must be set.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///   The <see cref="ResourceRecord.Name"/> of the <paramref name="key"/> is missing.
    /// </exception>
    public DSRecord(DNSKEYRecord key, bool force = false) 
        : this()
    {
        if (key.Algorithm is null)
            throw new ArgumentOutOfRangeException(nameof(key), "Algorithm is missing.");
        
        // Check the key.
        if (!force)
        {
            if ((key.Flags & DnsKeys.ZoneKey) == DnsKeys.None)
                throw new ArgumentException("ZoneKey must be set.", nameof(key));
            if ((key.Flags & DnsKeys.SecureEntryPoint) == DnsKeys.None)
                throw new ArgumentException("SecureEntryPoint must be set.", nameof(key));
        }

        byte[] digest;
        using (var ms = new MemoryStream())
        {
            var writer = new WireWriter(ms) { CanonicalForm = true };
            writer.WriteDomainName(key.Name);
            key.WriteData(writer);
            ms.Position = 0;
            digest = DigestRegistry.HashData(key.Algorithm.Value, ms);
        }
        
        Algorithm = key.Algorithm;
        Class = key.Class;
        KeyTag = key.KeyTag();
        Name = key.Name;
        TTL = key.TTL;
        Digest = digest;
        HashAlgorithm = DigestType.Sha1;
    }

    /// <summary>
    ///   The tag of the referenced <see cref="DNSKEYRecord"/>.
    /// </summary>
    public ushort? KeyTag { get; set; }

    /// <summary>
    ///   The <see cref="SecurityAlgorithm"/> of the referenced <see cref="DNSKEYRecord"/>.
    /// </summary>
    public SecurityAlgorithm? Algorithm {get; set; }

    /// <summary>
    ///   The cryptographic hash algorithm used to create the 
    ///   <see cref="Digest"/>.
    /// </summary>
    /// <value>
    ///   One of the <see cref="DigestType"/> value.
    /// </value>
    public DigestType? HashAlgorithm { get; set; }

    /// <summary>
    ///   The digest of the referenced <see cref="DNSKEYRecord"/>.
    /// </summary>
    /// <remarks>
    ///   <c>digest = HashAlgorithm(DNSKEY owner name | DNSKEY RDATA)</c>
    /// </remarks>
    public byte[]? Digest { get; set; }

    /// <inheritdoc />
    public override void ReadData(WireReader reader, int length)
    {
        var end = reader.Position + length;

        KeyTag = reader.ReadUInt16();
        Algorithm = (SecurityAlgorithm)reader.ReadByte();
        HashAlgorithm = (DigestType)reader.ReadByte();
        Digest = reader.ReadBytes(end - reader.Position);
    }

    /// <inheritdoc />
    public override void WriteData(WireWriter writer)
    {
        if (KeyTag is null)
            throw new InvalidOperationException("KeyTag is missing.");
        
        if (Algorithm is null)
            throw new InvalidOperationException("Algorithm is missing.");
        
        if (HashAlgorithm is null)
            throw new InvalidOperationException("HashAlgorithm is missing.");
        
        writer.WriteUInt16(KeyTag.Value);
        writer.WriteByte((byte)Algorithm);
        writer.WriteByte((byte)HashAlgorithm);
        writer.WriteBytes(Digest);
    }

    /// <inheritdoc />
    public override void ReadData(PresentationReader reader)
    {
        KeyTag = reader.ReadUInt16();
        Algorithm = (SecurityAlgorithm)reader.ReadByte();
        HashAlgorithm = (DigestType)reader.ReadByte();

        // Whitespace is allowed within the hexadecimal text.
        var sb = new StringBuilder();
        while (!reader.IsEndOfLine())
            sb.Append(reader.ReadString());
        
        Digest = BaseConvert.FromBase16(sb.ToString());
    }

    /// <inheritdoc />
    public override void WriteData(PresentationWriter writer)
    {
        if (KeyTag is null)
            throw new InvalidOperationException("KeyTag is missing.");
        
        if (Algorithm is null)
            throw new InvalidOperationException("Algorithm is missing.");
        
        if (HashAlgorithm is null)
            throw new InvalidOperationException("HashAlgorithm is missing.");
        
        if (Digest is null)
            throw new InvalidOperationException("Digest is missing.");
        
        writer.WriteUInt16(KeyTag.Value);
        writer.WriteByte((byte)Algorithm);
        writer.WriteByte((byte)HashAlgorithm);
        writer.WriteBase16String(Digest, appendSpace: false);
    }
}