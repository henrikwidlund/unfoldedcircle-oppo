using System.Security.Cryptography;

namespace Makaretu.Dns;

/// <summary>
///   Public key cryptography to sign and authenticate resource records.
/// </summary>
public class DNSKEYRecord : ResourceRecord
{
    /// <summary>
    ///   Creates a new instance of the <see cref="DNSKEYRecord"/> class.
    /// </summary>
    public DNSKEYRecord() => Type = DnsType.DNSKEY;

    /// <summary>
    ///   Creates a new instance of the <see cref="DNSKEYRecord"/> class
    ///   from the specified RSA key.
    /// </summary>
    /// <param name="key">
    ///   A public or private RSA key.
    /// </param>
    /// <param name="algorithm">
    ///   The security algorithm to use.  Only RSA types are allowed.
    /// </param>
    public DNSKEYRecord(RSA key, SecurityAlgorithm algorithm)
    {
        if (algorithm is not SecurityAlgorithm.RSAMD5 and
            not SecurityAlgorithm.RSASHA1 and
            not SecurityAlgorithm.RSASHA1NSEC3SHA1 and
            not SecurityAlgorithm.RSASHA256 and
            not SecurityAlgorithm.RSASHA512)
            throw new ArgumentException($"Security algorithm '{algorithm}' is not allowed for a RSA key.", nameof(algorithm));
        
        Algorithm = algorithm;

        using var ms = new MemoryStream();
        var p = key.ExportParameters(includePrivateParameters: false);
        if (p.Exponent is null)
            throw new InvalidOperationException("The RSA key does not have an exponent.");
            
        if (p.Modulus is null)
            throw new InvalidOperationException("The RSA key does not have a modulus.");

        ms.WriteByte((byte)p.Exponent.Length);
        ms.Write(p.Exponent, 0, p.Exponent.Length);
        ms.Write(p.Modulus, 0, p.Modulus.Length);
        PublicKey = ms.ToArray();
    }
    
    /// <summary>
    ///   Creates a new instance of the <see cref="DNSKEYRecord"/> class
    ///   from the specified ECDSA key.
    /// </summary>
    /// <param name="key">
    ///   A public or private ECDSA key.
    /// </param>
    /// <exception cref="ArgumentException">
    ///   <paramref name="key"/> is not named nistP256 nor nist384.
    /// </exception>
    /// <exception cref="CryptographicException">
    ///   <paramref name="key"/> is not valid.
    /// </exception>
    /// <remarks>
    ///   <note>
    ///   ECDSA key support is <b>NOT available</b> for NETSTANDARD14 nor NET45.
    ///   It is available for NETSTANDARD2, NET472 or greater.
    ///   </note>
    /// </remarks>
    public DNSKEYRecord(ECDsa key)
    {
        var p = key.ExportParameters(includePrivateParameters: false);
        p.Validate();
        
        if (p.Q.Y is null)
            throw new InvalidOperationException("The ECDSA key does not have a Y value.");
        
        if (p.Q.X is null)
            throw new InvalidOperationException("The ECDSA key does not have a X value.");

        if (!p.Curve.IsNamed)
            throw new ArgumentException("Only named ECDSA curves are allowed.", nameof(key));
        
        Algorithm = SecurityAlgorithmRegistry.Algorithms
            .Where(alg => alg.Value.OtherNames.Contains(p.Curve.Oid.FriendlyName, StringComparer.Ordinal))
            .Select(static alg => alg.Key)
            .FirstOrDefault();
        
        if (Algorithm == 0)
            throw new ArgumentException($"ECDSA curve '{p.Curve.Oid.FriendlyName} is not known'.", nameof(key));

        // ECDSA public keys consist of a single value, called "Q" in FIPS 186-3.
        // In DNSSEC keys, Q is a simple bit string that represents the
        // uncompressed form of a curve point, "x | y".
        using var ms = new MemoryStream();
        ms.Write(p.Q.X, 0, p.Q.X.Length);
        ms.Write(p.Q.Y, 0, p.Q.Y.Length);
        PublicKey = ms.ToArray();
    }

    /// <summary>
    ///  Identifies the intended usage of the key.
    /// </summary>
    public DnsKeys? Flags { get; set; }

    /// <summary>
    ///   Must be three.
    /// </summary>
    /// <value>
    ///   Defaults to 3.
    /// </value>
    public byte Protocol { get; set; } = 3;

    /// <summary>
    ///   Identifies the public key's cryptographic algorithm.
    /// </summary>
    /// <value>
    ///   Identifies the type of key (RSA, ECDSA, ...) and the
    ///   hashing algorithm.
    /// </value>
    /// <remarks>
    ///    Determines the format of the<see cref="PublicKey"/>.
    /// </remarks>
    public SecurityAlgorithm? Algorithm { get; set; }

    /// <summary>
    ///   The public key material.
    /// </summary>
    /// <value>
    ///   The format depends on the key <see cref="Algorithm"/>.
    /// </value>
    public byte[]? PublicKey { get; set; }

    /// <summary>
    ///   Calculates the key tag.
    /// </summary>
    /// <value>
    ///   A non-unique identifier for the public key.
    /// </value>
    /// <remarks>
    ///   <see href="https://tools.ietf.org/html/rfc4034#appendix-B"/> for the details.
    /// </remarks>
    public ushort KeyTag()
    {
        var key = GetData();
        var length = key.Length;
        var ac = 0;

        for (var i = 0; i < length; ++i)
            ac += (i & 1) == 1 ? key[i] : key[i] << 8;
        
        ac += (ac >> 16) & 0xFFFF;
        return (ushort) (ac & 0xFFFF);
    }

    /// <inheritdoc />
    public override void ReadData(WireReader reader, int length)
    {
        var end = reader.Position + length;

        Flags = (DnsKeys)reader.ReadUInt16();
        Protocol = reader.ReadByte();
        Algorithm = (SecurityAlgorithm)reader.ReadByte();
        PublicKey = reader.ReadBytes(end - reader.Position);
    }

    /// <inheritdoc />
    public override void WriteData(WireWriter writer)
    {
        writer.WriteUInt16(Flags.HasValue ? (ushort)Flags : default);
        writer.WriteByte(Protocol);
        writer.WriteByte(Algorithm.HasValue ? (byte)Algorithm : default);
        writer.WriteBytes(PublicKey);
    }

    /// <inheritdoc />
    public override void ReadData(PresentationReader reader)
    {
        Flags = (DnsKeys)reader.ReadUInt16();
        Protocol = reader.ReadByte();
        Algorithm = (SecurityAlgorithm)reader.ReadByte();
        PublicKey = reader.ReadBase64String();
    }

    /// <inheritdoc />
    public override void WriteData(PresentationWriter writer)
    {
        writer.WriteUInt16(Flags.HasValue ? (ushort)Flags : default);
        writer.WriteByte(Protocol);
        writer.WriteByte(Algorithm.HasValue ? (byte)Algorithm : default);
        writer.WriteBase64String(PublicKey ?? [], appendSpace: false);
    }
}