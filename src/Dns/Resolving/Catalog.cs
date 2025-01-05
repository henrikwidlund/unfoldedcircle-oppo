using System.Collections.Concurrent;
using System.Reflection;

namespace Makaretu.Dns.Resolving;

/// <summary>
///   A dictionary of <see cref="Node">DNS nodes</see>.
/// </summary>
/// <remarks>
///   This is a portion of the DNS distribute database.
///   <para>
///   The key is the case-insensitive <see cref="Node.Name"/> and the value is a <see cref="Node"/>.
///   </para>
/// </remarks>
public class Catalog : ConcurrentDictionary<DomainName, Node>
{
    /// <summary>
    ///   Include the zone information.
    /// </summary>
    /// <param name="reader">
    ///   The source of the zone information.
    /// </param>
    /// <returns>
    ///   The <see cref="Node"/> that represents the zone.
    /// </returns>
    /// <remarks>
    ///   All included nodes are marked as <see cref="Node.Authoritative"/>.
    /// </remarks>
    public Node IncludeZone(PresentationReader reader)
    {
        // Read the resources.
        var resources = new List<ResourceRecord>();
        while (true)
        {
            var r = reader.ReadResourceRecord();
            if (r is null)
                break;
            
            resources.Add(r);
        }

        // Validation
        if (resources.Count == 0)
            throw new InvalidDataException("No resources.");
        
        if (resources[0].Type != DnsType.SOA)
            throw new InvalidDataException("First resource record must be a SOA.");
        
        var soa = (SOARecord)resources[0];
        if (soa.Name is null)
            throw new InvalidDataException("SOA name is missing.");
        
        if (resources.Any(r => !r.Name?.BelongsTo(soa.Name!) ?? false))
            throw new InvalidDataException("All resource records must belong to the zone.");

        // Insert the nodes of the zone.
        var nodes = resources.Where(static r => r.Name is not null)
            .GroupBy(static r => r.Name, static (key, results) => new Node
            {
                Name = key!,
                Authoritative = true,
                Resources = new ConcurrentSet<ResourceRecord>(results)
            }
        );
        
        foreach (var node in nodes)
        {
            if (!TryAdd(node.Name, node))
                throw new InvalidDataException($"'{node.Name}' already exists.");
        }

        return this[soa.Name];
    }

    /// <summary>
    ///   Remove all nodes that belong to the zone.
    /// </summary>
    /// <param name="name">
    ///   The name of the zone.
    /// </param>
    public void RemoveZone(DomainName name)
    {
        var keys = Keys.Where(k => k.BelongsTo(name));
        foreach (var key in keys)
            TryRemove(key, out _);
    }

    /// <summary>
    ///   Add or update the resource record to the catalog. 
    /// </summary>
    /// <param name="resource">
    ///   The <see cref="ResourceRecord.Name"/> is also the name of the node.
    /// </param>
    /// <param name="authoritative">
    ///   Indicates if the <paramref name="resource"/> is authoritative or cached.
    ///   Only used when a <see cref="Node"/> is created.
    /// </param>
    /// <returns>
    ///   The <see cref="Node"/> that was created or updated.
    /// </returns>
    /// <remarks>
    ///   If the <paramref name="resource"/> already exists, then update the
    ///   non-equality properties <see cref="ResourceRecord.TTL"/>
    ///   and <see cref="DnsObject.CreationTime"/>.
    /// </remarks>
    public Node Add(ResourceRecord resource, bool authoritative = false)
    {
        if (resource.Name is null)
            throw new ArgumentNullException(nameof(resource), "Resource name is missing.");
        var node = AddOrUpdate(
            resource.Name, static (k, arg) => new Node { Name = k, Authoritative = arg },
            static (_, n, _) => n,
            authoritative
        );

        // If the resource already exist, then update the the non-equality
        // properties TTL and CreationTime.
        if (!node.Resources.Add(resource))
        {
            node.Resources.Remove(resource);
            node.Resources.Add(resource);
        }

        return node;
    }

    /// <summary>
    ///   Include the root name servers.
    /// </summary>
    /// <returns>
    ///   The <see cref="Node"/> that represents the "root".
    /// </returns>
    /// <remarks>
    ///   A DNS recursive resolver typically needs a "root hints file". This file 
    ///   contains the names and IP addresses of the authoritative name servers for the root zone, 
    ///   so the software can bootstrap the DNS resolution process.
    /// </remarks>
    public Node IncludeRootHints()
    {
        var assembly = typeof(Catalog).GetTypeInfo().Assembly;
        using (var hints = assembly.GetManifestResourceStream("Makaretu.Dns.Resolving.RootHints"))
        {
            if (hints is null)
                throw new InvalidOperationException("RootHints not found.");
            
            var reader = new PresentationReader(new StreamReader(hints));
            while (reader.ReadResourceRecord() is { } r)
                Add(r);
        }

        var root = this[new DomainName("")];
        root.Authoritative = true;
        return root;
    }

    /// <summary>
    ///   Include the resource records.
    /// </summary>
    /// <param name="reader">
    ///   The source of the resource records.
    /// </param>
    /// <param name="authoritative">
    ///   Indicates if a <see cref="ResourceRecord"/> is authoritative or cached.
    ///   Only used when a <see cref="Node"/> is created.
    /// </param>
    public void Include(PresentationReader reader, bool authoritative = false)
    {
        while (true)
        {
            var r = reader.ReadResourceRecord();
            if (r is null)
                break;
            
            Add(r, authoritative);
        }
    }

    /// <summary>
    ///   Get a sequence of nodes in canonical order.
    /// </summary>
    /// <returns>
    ///   A sequence of nodes in canonical order.
    /// </returns>
    /// <remarks>
    ///   Node names are converted to US-ASCII lowercase and
    ///   then sorted by their reversed labels.
    /// </remarks>
    public IEnumerable<Node> NodesInCanonicalOrder() =>
        Values
            .OrderBy(static node =>
            {
                var co = node.Name.ToCanonical().Labels.Reverse().ToArray();
                var coname = new DomainName(co);
                return coname.ToString();
            }, StringComparer.Ordinal);

    /// <summary>
    ///   Add PTR records for each authoritative A/AAAA record.
    /// </summary>
    /// <remarks>
    ///   This enables reverse DNS lookup of all address records.
    /// </remarks>
    public void IncludeReverseLookupRecords()
    {
        var addressRecords = this.Values
            .Where(static node => node.Authoritative)
            .SelectMany(static node => node.Resources.OfType<AddressRecord>())
            .Where(static a => a.Address is not null);
        
        foreach (var a in addressRecords)
        {
            var ptr = new PTRRecord
            {
                Class = a.Class,
                Name = new DomainName(a.Address!.GetArpaName()),
                DomainName = a.Name,
                TTL = a.TTL
            };
            Add(ptr, authoritative:  true);
        }
    }
}