namespace Makaretu.Dns;

/// <summary>
/// A resource record or query type. 
/// </summary>
/// <seealso cref="Question.Type"/>
/// <seealso cref="ResourceRecord.Type"/>
public enum DnsType : ushort
{
    /// <summary>
    /// A host address.
    /// </summary>
    /// <seealso href="https://tools.ietf.org/html/rfc1035">RFC 1035</seealso>
    /// <seealso cref="ARecord"/>
    A = 1,

    /// <summary>
    /// An authoritative name server.
    /// </summary>
    /// <seealso href="https://tools.ietf.org/html/rfc1035#section-3.3.11">RFC 1035</seealso>
    /// <seealso cref="NSRecord"/>
    NS = 2,

    /// <summary>
    /// A mail destination (OBSOLETE - use MX).
    /// </summary>
    /// <seealso href="https://tools.ietf.org/html/rfc1035">RFC 1035</seealso>
    [Obsolete("Use MX")]
    MD = 3,

    /// <summary>
    /// A mail forwarder (OBSOLETE - use MX).
    /// </summary>
    /// <seealso href="https://tools.ietf.org/html/rfc1035">RFC 1035</seealso>
    [Obsolete("Use MX")]
    MF = 4,

    /// <summary>
    /// The canonical name for an alias.
    /// </summary>
    /// <seealso href="https://tools.ietf.org/html/rfc1035#section-3.3.1">RFC 1035</seealso>
    /// <seealso cref="CNAMERecord"/>
    CNAME = 5,

    /// <summary>
    /// Marks the start of a zone of authority.
    /// </summary>
    /// <seealso href="https://tools.ietf.org/html/rfc1035#section-3.3.13">RFC 1035</seealso>
    /// <seealso cref="SOARecord"/>
    SOA = 6,

    /// <summary>
    /// A mailbox domain name (EXPERIMENTAL).
    /// </summary>
    /// <seealso href="https://tools.ietf.org/html/rfc1035#section-3.3.3">RFC 1035</seealso>
    MB = 7,

    /// <summary>
    /// A mail group member (EXPERIMENTAL).
    /// </summary>
    /// <seealso href="https://tools.ietf.org/html/rfc1035#section-3.3.6">RFC 1035</seealso>
    MG = 8,

    /// <summary>
    /// A mailbox rename domain name (EXPERIMENTAL).
    /// </summary>
    /// <seealso href="https://tools.ietf.org/html/rfc1035#section-3.3.8">RFC 1035</seealso>
    MR = 9,

    /// <summary>
    /// A Null resource record (EXPERIMENTAL).
    /// </summary>
    /// <seealso href="https://tools.ietf.org/html/rfc1035#section-3.3.8">RFC 1035</seealso>
    /// <seealso cref="NULLRecord"/>
    NULL = 10,

    /// <summary>
    /// A well known service description.
    /// </summary>
    /// <seealso href="https://tools.ietf.org/html/rfc3232">RFC 3232</seealso>
    WKS = 11,

    /// <summary>
    /// A domain name pointer.
    /// </summary>
    /// <seealso href="https://tools.ietf.org/html/rfc1035#section-3.3.12">RFC 1035</seealso>
    /// <seealso cref="PTRRecord"/>
    PTR = 12,

    /// <summary>
    /// Host information.
    /// </summary>
    /// <seealso href="https://tools.ietf.org/html/rfc1035#section-3.3.11">RFC 1035</seealso>
    /// <seealso href="https://tools.ietf.org/html/rfc1010">RFC 1010</seealso>
    /// <seealso cref="HINFORecord"/>
    HINFO = 13,

    /// <summary>
    /// Mailbox or mail list information.
    /// </summary>
    /// <seealso href="https://tools.ietf.org/html/rfc1035#section-3.3.11">RFC 1035</seealso>
    MINFO = 14,

    /// <summary>
    /// Mail exchange.
    /// </summary>
    /// <seealso href="https://tools.ietf.org/html/rfc1035#section-3.3.9">RFC 1035</seealso>
    /// <seealso href="https://tools.ietf.org/html/rfc974">RFC 974</seealso>
    /// <seealso cref="MXRecord"/>
    MX = 15,

    /// <summary>
    /// Text resources.
    /// </summary>
    /// <seealso href="https://tools.ietf.org/html/rfc1035#section-3.3">RFC 1035</seealso>
    /// <seealso href="https://tools.ietf.org/html/rfc1464">RFC 1464</seealso>
    /// <seealso cref="TXTRecord"/>
    TXT = 16,

    /// <summary>
    /// Responsible Person.
    /// </summary>
    /// <seealso href="https://tools.ietf.org/html/rfc1183">RFC 1183</seealso>
    /// <seealso cref="RPRecord"/>
    RP = 17,

    /// <summary>
    /// AFS Data Base location.
    /// </summary>
    /// <seealso href="https://tools.ietf.org/html/rfc1183#section-1">RFC 1183</seealso>
    /// <seealso href="https://tools.ietf.org/html/rfc5864">RFC 5864</seealso>
    /// <seealso cref="AFSDBRecord"/>
    AFSDB = 18,

    /// <summary>
    ///  X.25 PSDN address.
    /// </summary>
    /// <seealso href="https://tools.ietf.org/html/rfc1183#section-1">RFC 1183</seealso>
    X25 = 19,

    /// <summary>
    ///  ISDN address.
    /// </summary>
    /// <seealso href="https://tools.ietf.org/html/rfc1183#section-1">RFC 1183</seealso>
    ISDN = 20,

    /// <summary>
    ///  Route Through.
    /// </summary>
    /// <seealso href="https://tools.ietf.org/html/rfc1183#section-1">RFC 1183</seealso>
    RT = 21,

    /// <summary>
    ///  NSAP style A record (Historic)
    /// </summary>
    [Obsolete("Historic")]
    NSAP = 22,

    /// <summary>
    ///  Domain name pointer, NSAP style (Historic)
    /// </summary>
    [Obsolete("Historic")]
    NSAPPTR = 23,

    /// <summary>
    /// Security signature
    /// </summary>
    /// <seealso href="https://tools.ietf.org/html/rfc4034">RFC 4034</seealso>
    SIG = 24,

    /// <summary>
    /// Security Key
    /// </summary>
    /// <seealso href="https://tools.ietf.org/html/rfc4034">RFC 4034</seealso>
    KEY = 25,

    /// <summary>
    /// X.400 mail mapping information
    /// </summary>
    /// <seealso href="https://tools.ietf.org/html/rfc2163">RFC 2163</seealso>
    PX = 26,

    /// <summary>
    /// Geographical Position
    /// </summary>
    /// <seealso href="https://tools.ietf.org/html/rfc1712">RFC 1712</seealso>
    GPOS = 27,

    /// <summary>
    /// An IPv6 host address.
    /// </summary>
    /// <seealso href="https://tools.ietf.org/html/rfc3596#section-2.2">RFC 3596</seealso>
    /// <seealso cref="AAAARecord"/>
    AAAA = 28,

    /// <summary>
    /// Location Information
    /// </summary>
    /// <seealso href="https://tools.ietf.org/html/rfc1876">RFC 1876</seealso>
    LOC = 29,

    /// <summary>
    /// A resource record which specifies the location of the server(s) for a specific protocol and domain.
    /// </summary>
    /// <seealso href="https://tools.ietf.org/html/rfc2782">RFC 2782</seealso>
    /// <seealso cref="SRVRecord"/>
    SRV = 33,

    /// <summary>
    ///   Maps an entire domain name.
    /// </summary>
    /// <seealso href="https://tools.ietf.org/html/rfc6672">RFC 6672</seealso>
    /// <see cref="DNAMERecord"/>
    DNAME = 39,

    /// <summary>
    /// Option record.
    /// </summary>
    /// <seealso href="https://tools.ietf.org/html/rfc6891">RFC 6891</seealso>
    /// <see cref="OPTRecord"/>
    OPT = 41,

    /// <summary>
    ///   Delegation Signer.
    /// </summary>
    /// <seealso href="https://tools.ietf.org/html/rfc4034#section-5"/>
    /// <see cref="DSRecord"/>
    DS = 43,

    /// <summary>
    /// Signature for a RRSET with a particular name, class, and type.
    /// </summary>
    /// <seealso href="https://tools.ietf.org/html/rfc4034#section-3"/>
    /// <seealso cref="RRSIGRecord"/>
    RRSIG = 46,

    /// <summary>
    ///   Next secure owener.
    /// </summary>
    /// <seealso href="https://tools.ietf.org/html/rfc3845"/>
    /// <seealso cref="NSECRecord"/>
    NSEC = 47,

    /// <summary>
    ///   Public key cryptography to sign and authenticate resource records.
    /// </summary>
    /// <seealso href="https://tools.ietf.org/html/rfc4034#section-2.1"/>
    /// <seealso cref="DNSKEYRecord"/>
    DNSKEY = 48,

    /// <summary>
    ///   Authenticated next secure owner.
    /// </summary>
    /// <seealso href="https://tools.ietf.org/html/rfc5155"/>
    /// <seealso cref="NSEC3Record"/>
    NSEC3 = 50,

    /// <summary>
    ///   Parameters needed by authoritative servers to calculate hashed owner names.
    /// </summary>
    /// <seealso href="https://tools.ietf.org/html/rfc5155#section-4"/>
    /// <seealso cref="NSEC3PARAMRecord"/>
    NSEC3PARAM = 51,

    /// <summary>
    ///   Shared secret key.
    /// </summary>
    /// <seealso href="https://tools.ietf.org/html/rfc2930"/>
    /// <seealso cref="TKEYRecord"/>
    TKEY = 249,

    /// <summary>
    ///  Transactional Signature.
    /// </summary>
    /// <seealso href="https://tools.ietf.org/html/rfc2845"/>
    /// <seealso cref="TSIGRecord"/>
    TSIG = 250,

    /// <summary>
    /// A request for a transfer of an entire zone.
    /// </summary>
    /// <seealso href="https://tools.ietf.org/html/rfc1035">RFC 1035</seealso>
    AXFR = 252,

    /// <summary>
    ///  A request for mailbox-related records (MB, MG or MR).
    /// </summary>
    /// <seealso href="https://tools.ietf.org/html/rfc1035">RFC 1035</seealso>
    MAILB = 253,

    /// <summary>
    ///  A request for mail agent RRs (Obsolete - see MX).
    /// </summary>
    /// <seealso href="https://tools.ietf.org/html/rfc1035">RFC 1035</seealso>
    [Obsolete("Use MX")]
    MAILA = 254,

    /// <summary>
    ///  A request for any record(s).
    /// </summary>
    /// <seealso href="https://tools.ietf.org/html/rfc1035">RFC 1035</seealso>
    ANY = 255,

    /// <summary>
    /// A Uniform Resource Identifier (URI) resource record.
    /// </summary>
    /// <seealso href="https://tools.ietf.org/html/rfc7553">RFC 7553</seealso>
    URI = 256,

    /// <summary>
    /// A certification authority authorization.
    /// </summary>
    /// <seealso href="https://tools.ietf.org/html/rfc6844">RFC 6844</seealso>
    CAA = 257
}

public static class DnsTypeExtensions
{
#pragma warning disable CS0618 // Type or member is obsolete
    
    public static DnsType Parse(
        [System.Diagnostics.CodeAnalysis.NotNullWhen(true)]
        string? name) =>
        name switch
        {
            nameof(DnsType.A) => DnsType.A,
            nameof(DnsType.NS) => DnsType.NS,
            nameof(DnsType.MD) => DnsType.MD,
            nameof(DnsType.MF) => DnsType.MF,
            nameof(DnsType.CNAME) => DnsType.CNAME,
            nameof(DnsType.SOA) => DnsType.SOA,
            nameof(DnsType.MB) => DnsType.MB,
            nameof(DnsType.MG) => DnsType.MG,
            nameof(DnsType.MR) => DnsType.MR,
            nameof(DnsType.NULL) => DnsType.NULL,
            nameof(DnsType.WKS) => DnsType.WKS,
            nameof(DnsType.PTR) => DnsType.PTR,
            nameof(DnsType.HINFO) => DnsType.HINFO,
            nameof(DnsType.MINFO) => DnsType.MINFO,
            nameof(DnsType.MX) => DnsType.MX,
            nameof(DnsType.TXT) => DnsType.TXT,
            nameof(DnsType.RP) => DnsType.RP,
            nameof(DnsType.AFSDB) => DnsType.AFSDB,
            nameof(DnsType.X25) => DnsType.X25,
            nameof(DnsType.ISDN) => DnsType.ISDN,
            nameof(DnsType.RT) => DnsType.RT,
            nameof(DnsType.NSAP) => DnsType.NSAP,
            nameof(DnsType.NSAPPTR) => DnsType.NSAPPTR,
            nameof(DnsType.SIG) => DnsType.SIG,
            nameof(DnsType.KEY) => DnsType.KEY,
            nameof(DnsType.PX) => DnsType.PX,
            nameof(DnsType.GPOS) => DnsType.GPOS,
            nameof(DnsType.AAAA) => DnsType.AAAA,
            nameof(DnsType.LOC) => DnsType.LOC,
            nameof(DnsType.SRV) => DnsType.SRV,
            nameof(DnsType.DNAME) => DnsType.DNAME,
            nameof(DnsType.OPT) => DnsType.OPT,
            nameof(DnsType.DS) => DnsType.DS,
            nameof(DnsType.RRSIG) => DnsType.RRSIG,
            nameof(DnsType.NSEC) => DnsType.NSEC,
            nameof(DnsType.DNSKEY) => DnsType.DNSKEY,
            nameof(DnsType.NSEC3) => DnsType.NSEC3,
            nameof(DnsType.NSEC3PARAM) => DnsType.NSEC3PARAM,
            nameof(DnsType.TKEY) => DnsType.TKEY,
            nameof(DnsType.TSIG) => DnsType.TSIG,
            nameof(DnsType.AXFR) => DnsType.AXFR,
            nameof(DnsType.MAILB) => DnsType.MAILB,
            nameof(DnsType.MAILA) => DnsType.MAILA,
            nameof(DnsType.ANY) => DnsType.ANY,
            nameof(DnsType.URI) => DnsType.URI,
            nameof(DnsType.CAA) => DnsType.CAA,
            not null when ushort.TryParse(name, out var val) => (DnsType)val,
            _ => throw new ArgumentException($"Requested value '{name}' was not found.", nameof(name))
        };

    public static bool IsDefined(DnsType value)
        => value switch
        {
            DnsType.A => true,
            DnsType.NS => true,
            DnsType.MD => true,
            DnsType.MF => true,
            DnsType.CNAME => true,
            DnsType.SOA => true,
            DnsType.MB => true,
            DnsType.MG => true,
            DnsType.MR => true,
            DnsType.NULL => true,
            DnsType.WKS => true,
            DnsType.PTR => true,
            DnsType.HINFO => true,
            DnsType.MINFO => true,
            DnsType.MX => true,
            DnsType.TXT => true,
            DnsType.RP => true,
            DnsType.AFSDB => true,
            DnsType.X25 => true,
            DnsType.ISDN => true,
            DnsType.RT => true,
            DnsType.NSAP => true,
            DnsType.NSAPPTR => true,
            DnsType.SIG => true,
            DnsType.KEY => true,
            DnsType.PX => true,
            DnsType.GPOS => true,
            DnsType.AAAA => true,
            DnsType.LOC => true,
            DnsType.SRV => true,
            DnsType.DNAME => true,
            DnsType.OPT => true,
            DnsType.DS => true,
            DnsType.RRSIG => true,
            DnsType.NSEC => true,
            DnsType.DNSKEY => true,
            DnsType.NSEC3 => true,
            DnsType.NSEC3PARAM => true,
            DnsType.TKEY => true,
            DnsType.TSIG => true,
            DnsType.AXFR => true,
            DnsType.MAILB => true,
            DnsType.MAILA => true,
            DnsType.ANY => true,
            DnsType.URI => true,
            DnsType.CAA => true,
            _ => false
        };

    public static string ToStringFast(this DnsType value)
        => value switch
        {
            DnsType.A => nameof(DnsType.A),
            DnsType.NS => nameof(DnsType.NS),
            DnsType.MD => nameof(DnsType.MD),
            DnsType.MF => nameof(DnsType.MF),
            DnsType.CNAME => nameof(DnsType.CNAME),
            DnsType.SOA => nameof(DnsType.SOA),
            DnsType.MB => nameof(DnsType.MB),
            DnsType.MG => nameof(DnsType.MG),
            DnsType.MR => nameof(DnsType.MR),
            DnsType.NULL => nameof(DnsType.NULL),
            DnsType.WKS => nameof(DnsType.WKS),
            DnsType.PTR => nameof(DnsType.PTR),
            DnsType.HINFO => nameof(DnsType.HINFO),
            DnsType.MINFO => nameof(DnsType.MINFO),
            DnsType.MX => nameof(DnsType.MX),
            DnsType.TXT => nameof(DnsType.TXT),
            DnsType.RP => nameof(DnsType.RP),
            DnsType.AFSDB => nameof(DnsType.AFSDB),
            DnsType.X25 => nameof(DnsType.X25),
            DnsType.ISDN => nameof(DnsType.ISDN),
            DnsType.RT => nameof(DnsType.RT),
            DnsType.NSAP => nameof(DnsType.NSAP),
            DnsType.NSAPPTR => nameof(DnsType.NSAPPTR),
            DnsType.SIG => nameof(DnsType.SIG),
            DnsType.KEY => nameof(DnsType.KEY),
            DnsType.PX => nameof(DnsType.PX),
            DnsType.GPOS => nameof(DnsType.GPOS),
            DnsType.AAAA => nameof(DnsType.AAAA),
            DnsType.LOC => nameof(DnsType.LOC),
            DnsType.SRV => nameof(DnsType.SRV),
            DnsType.DNAME => nameof(DnsType.DNAME),
            DnsType.OPT => nameof(DnsType.OPT),
            DnsType.DS => nameof(DnsType.DS),
            DnsType.RRSIG => nameof(DnsType.RRSIG),
            DnsType.NSEC => nameof(DnsType.NSEC),
            DnsType.DNSKEY => nameof(DnsType.DNSKEY),
            DnsType.NSEC3 => nameof(DnsType.NSEC3),
            DnsType.NSEC3PARAM => nameof(DnsType.NSEC3PARAM),
            DnsType.TKEY => nameof(DnsType.TKEY),
            DnsType.TSIG => nameof(DnsType.TSIG),
            DnsType.AXFR => nameof(DnsType.AXFR),
            DnsType.MAILB => nameof(DnsType.MAILB),
            DnsType.MAILA => nameof(DnsType.MAILA),
            DnsType.ANY => nameof(DnsType.ANY),
            DnsType.URI => nameof(DnsType.URI),
            DnsType.CAA => nameof(DnsType.CAA),
            _ => value.ToString()
        };

#pragma warning restore CS0618 // Type or member is obsolete
}