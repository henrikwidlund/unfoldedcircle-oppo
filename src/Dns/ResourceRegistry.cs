namespace Makaretu.Dns;

/// <summary>
///   Metadata on resource records.
/// </summary>
/// <see cref="ResourceRecord"/>
public static class ResourceRegistry
{
    private static readonly Dictionary<DnsType, Func<ResourceRecord>> RecordsPrivate = InitRecords();
    
    private static Dictionary<DnsType, Func<ResourceRecord>> InitRecords()
    {
        var dictionary = new Dictionary<DnsType, Func<ResourceRecord>>();
        RegisterCore<ARecord>(dictionary);
        RegisterCore<AAAARecord>(dictionary);
        RegisterCore<AFSDBRecord>(dictionary);
        RegisterCore<CNAMERecord>(dictionary);
        RegisterCore<DNAMERecord>(dictionary);
        RegisterCore<DNSKEYRecord>(dictionary);
        RegisterCore<DSRecord>(dictionary);
        RegisterCore<HINFORecord>(dictionary);
        RegisterCore<MXRecord>(dictionary);
        RegisterCore<NSECRecord>(dictionary);
        RegisterCore<NSEC3Record>(dictionary);
        RegisterCore<NSEC3PARAMRecord>(dictionary);
        RegisterCore<NSRecord>(dictionary);
        RegisterCore<NULLRecord>(dictionary);
        RegisterCore<OPTRecord>(dictionary);
        RegisterCore<PTRRecord>(dictionary);
        RegisterCore<RPRecord>(dictionary);
        RegisterCore<RRSIGRecord>(dictionary);
        RegisterCore<SOARecord>(dictionary);
        RegisterCore<SRVRecord>(dictionary);
        RegisterCore<TKEYRecord>(dictionary);
        RegisterCore<TSIGRecord>(dictionary);
        RegisterCore<TXTRecord>(dictionary);

        return dictionary;
    }
    
    /// <summary>
    ///   All the resource records.
    /// </summary>
    /// <remarks>
    ///   The key is the DNS Resource Record type, <see cref="DnsType"/>.
    ///   The value is a function that returns a new <see cref="ResourceRecord"/>.
    /// </remarks>
    public static IReadOnlyDictionary<DnsType, Func<ResourceRecord>> Records => RecordsPrivate;

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
        RegisterCore<T>(RecordsPrivate);
    }
    
    private static void RegisterCore<T>(Dictionary<DnsType, Func<ResourceRecord>> records) where T : ResourceRecord, new()
    {
        var rr = new T();
        if (!DnsTypeExtensions.IsDefined(rr.Type))
            throw new InvalidOperationException($"The RR TYPE {rr.Type} is not defined.");
        
        records.Add(rr.Type, static () => new T());
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
    public static ResourceRecord Create(DnsType type) => RecordsPrivate.TryGetValue(type, out var maker) ? maker() : new UnknownRecord();
}