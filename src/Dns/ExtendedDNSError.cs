namespace Makaretu.Dns;

/// <summary>
/// RFC 8914
/// </summary>
public enum ExtendedDNSError : ushort
{
    /// <summary>
    /// The error in question falls into a category that does not match known extended error codes. Implementations SHOULD include an EXTRA-TEXT value to augment this error code with additional information.
    /// </summary>
    Other = 0x0,
    /// <summary>
    /// The resolver attempted to perform DNSSEC validation, but a DNSKEY RRset contained only unsupported DNSSEC algorithms.
    /// </summary>
    UnsupportedDNSKeyAlgorithm = 0x1,
    /// <summary>
    /// The resolver attempted to perform DNSSEC validation, but a DS RRset contained only unsupported Digest Types.
    /// </summary>
    UnsupportedDSDigestType = 0x2,
    /// <summary>
    /// The resolver was unable to resolve the answer within its time limits and decided to answer with previously cached data instead of answering with an error. This is typically caused by problems communicating with an authoritative server, possibly as result of a denial of service (DoS) attack against another network.
    /// </summary>
    StaleAnswer = 0x3,
    /// <summary>
    /// For policy reasons (legal obligation or malware filtering, for instance), an answer was forged.
    /// </summary>
    ForgedAnswer = 0x4,
    /// <summary>
    /// The resolver attempted to perform DNSSEC validation, but validation ended in the Indeterminate state
    /// </summary>
    DNSSECIndeterminate = 0x5,
    /// <summary>
    /// The resolver attempted to perform DNSSEC validation, but validation ended in the Bogus state.
    /// </summary>
    DNSSECBogus = 0x6,
    /// <summary>
    /// The resolver attempted to perform DNSSEC validation, but no signatures are presently valid and some (often all) are expired.
    /// </summary>
    SignatureExpired = 0x7,
    /// <summary>
    /// The resolver attempted to perform DNSSEC validation, but no signatures are presently valid and at least some are not yet valid.
    /// </summary>
    SignatureNotYetValid = 0x8,
    /// <summary>
    /// A DS record existed at a parent, but no supported matching DNSKEY record could be found for the child.
    /// </summary>
    DNSKeyMissing = 0x9,
    /// <summary>
    /// The resolver attempted to perform DNSSEC validation, but no RRSIGs could be found for at least one RRset where RRSIGs were expected.
    /// </summary>
    RRSIGsMissing = 0xA,
    /// <summary>
    /// The resolver attempted to perform DNSSEC validation, but no Zone Key Bit was set in a DNSKEY.
    /// </summary>
    NoZoneKeyBitSet = 0xB,
    /// <summary>
    /// The resolver attempted to perform DNSSEC validation, but the requested data was missing and a covering NSEC or NSEC3 was not provided.
    /// </summary>
    NSECMissing = 0xC,
    /// <summary>
    /// The resolver is returning the SERVFAIL RCODE from its cache.
    /// </summary>
    CachedError = 0xD,
    /// <summary>
    /// The server is unable to answer the query, as it was not fully functional when the query was received.
    /// </summary>
    NotReady = 0xE,
    /// <summary>
    /// The server is unable to respond to the request because the domain is on a blocklist due to an internal security policy imposed by the operator of the server resolving or forwarding the query.
    /// </summary>
    Blocked = 0xF,
    /// <summary>
    /// The server is unable to respond to the request because the domain is on a blocklist due to an external requirement imposed by an entity other than the operator of the server resolving or forwarding the query. 
    /// </summary>
    Censored = 0x10,
    /// <summary>
    /// The server is unable to respond to the request because the domain is on a blocklist as requested by the client.
    /// </summary>
    Filtered = 0x11,
    /// <summary>
    /// An authoritative server or recursive resolver that receives a query from an "unauthorized" client can annotate its REFUSED message with this code. 
    /// </summary>
    Prohibited = 0x12,
    /// <summary>
    /// The resolver was unable to resolve an answer within its configured time limits and decided to answer with a previously cached NXDOMAIN answer instead of answering with an error. 
    /// </summary>
    StaleNXDOMAINAnswer = 0x13,
    /// <summary>
    /// An authoritative server that receives a query with the Recursion Desired (RD) bit clear, or when it is not configured for recursion for a domain for which it is not authoritative, SHOULD include this EDE code in the REFUSED response.
    /// </summary>
    NotAuthoritative = 0x14,
    /// <summary>
    /// The requested operation or query is not supported.
    /// </summary>
    NotSupported = 0x15,
    /// <summary>
    /// The resolver could not reach any of the authoritative name servers (or they potentially refused to reply).
    /// </summary>
    NoReachableAuthority = 0x16,
    /// <summary>
    /// An unrecoverable error occurred while communicating with another server.
    /// </summary>
    NetworkError = 0x17,
    /// <summary>
    /// The authoritative server cannot answer with data for a zone it is otherwise configured to support. 
    /// </summary>
    InvalidData = 0x18
}