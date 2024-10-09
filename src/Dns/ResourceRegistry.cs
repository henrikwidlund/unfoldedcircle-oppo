namespace Makaretu.Dns;

/// <summary>
///   Metadata on resource records.
/// </summary>
/// <see cref="ResourceRecord"/>
public static class ResourceRegistry
{
    /// <summary>
    ///   All the resource records.
    /// </summary>
    /// <remarks>
    ///   The key is the DNS Resource Record type, <see cref="DnsType"/>.
    ///   The value is a function that returns a new <see cref="ResourceRecord"/>.
    /// </remarks>
    public static readonly Dictionary<DnsType, Func<ResourceRecord>> Records;

    static ResourceRegistry()
    {
        Records = new Dictionary<DnsType, Func<ResourceRecord>>();
        Register<ARecord>();
        Register<AAAARecord>();
        Register<AFSDBRecord>();
        Register<CNAMERecord>();
        Register<DNAMERecord>();
        Register<DNSKEYRecord>();
        Register<DSRecord>();
        Register<HINFORecord>();
        Register<MXRecord>();
        Register<NSECRecord>();
        Register<NSEC3Record>();
        Register<NSEC3PARAMRecord>();
        Register<NSRecord>();
        Register<NULLRecord>();
        Register<OPTRecord>();
        Register<PTRRecord>();
        Register<RPRecord>();
        Register<RRSIGRecord>();
        Register<SOARecord>();
        Register<SRVRecord>();
        Register<TKEYRecord>();
        Register<TSIGRecord>();
        Register<TXTRecord>();
    }

    /// <summary>
    ///   Register a new resource record.
    /// </summary>
    /// <typeparam name="T">
    ///   A derived class of <see cref="ResourceRecord"/>.
    /// </typeparam>
    /// <exception cref="ArgumentException">
    ///   When RR TYPE is zero.
    /// </exception>
    public static void Register<T>() where T : ResourceRecord, new()
    {
        var rr = new T();
        if (!DnsTypeExtensions.IsDefined(rr.Type))
            throw new InvalidOperationException($"The RR TYPE {rr.Type} is not defined.");
        
        Records.Add(rr.Type, static () => new T());
    }

    /// <summary>
    ///   Gets the resource record for the <see cref="DnsType"/>.
    /// </summary>
    /// <param name="type">
    ///   One of the <see cref="DnsType"/> values.
    /// </param>
    /// <returns>
    ///   A new instance derived from <see cref="ResourceRecord"/>.
    /// </returns>
    /// <remarks>
    ///   When the <paramref name="type"/> is not implemented, a new
    ///   of <see cref="UnknownRecord"/> is returned.
    /// </remarks>
    public static ResourceRecord Create(DnsType type) => Records.TryGetValue(type, out var maker) ? maker() : new UnknownRecord();
}