﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Makaretu.Dns;

/// <summary>
///   Defines a specific service that can be discovered.
/// </summary>
/// <seealso cref="ServiceDiscovery.Advertise(ServiceProfile)"/>
public class ServiceProfile
{
    private readonly DomainName _hostName;
    private readonly DomainName? _serviceName;
    private readonly DomainName? _instanceName;
    /// <summary>
    ///   Creates a new instance of the <see cref="ServiceProfile"/> class.
    /// </summary>
    /// <remarks>
    ///   All details must be filled in by the caller, especially the <see cref="Resources"/>.
    /// </remarks>
    public ServiceProfile()
    {
        _instanceName = new DomainName();
        _serviceName = new DomainName();
        _hostName = new DomainName();
    }

    /// <summary>
    ///   Creates a new instance of the <see cref="ServiceProfile"/> class
    ///   with the specified details.
    /// </summary>
    /// <param name="instanceName">
    ///    A unique identifier for the specific service instance.
    /// </param>
    /// <param name="serviceName">
    ///   The <see cref="ServiceName">name</see> of the service.
    /// </param>
    /// <param name="port">
    ///   The TCP/UDP port of the service.
    /// </param>
    /// <param name="addresses">
    ///   The IP addresses of the specific service instance. If <b>null</b> then
    ///   <see cref="MulticastService.GetIPAddresses"/> is used.
    /// </param>
    /// <param name="sharedProfile">
    ///     If set, this profile is shared between multiple mDNS responders.
    ///     This server does not own the profile exclusively.
    /// </param>
    /// <remarks>
    ///   The SRV, TXT and A/AAAA resoruce records are added to the <see cref="Resources"/>.
    /// </remarks>
    public ServiceProfile(DomainName instanceName, DomainName serviceName, ushort port, IEnumerable<IPAddress>? addresses = null, bool sharedProfile = false)
    {
        _instanceName = instanceName;
        _serviceName = serviceName;
        SharedProfile = sharedProfile;

        var simpleServiceName = new DomainName(ServiceName!.ToString()
            .Replace("._tcp", "", StringComparison.Ordinal)
            .Replace("._udp", "", StringComparison.Ordinal)
            .Trim('_')
            .Replace('_', '-'));
        _hostName = DomainName.Join(InstanceName!, simpleServiceName, Domain);
        Resources.Add(new SRVRecord
        {
            Name = FullyQualifiedName,
            Port = port,
            Target = HostName,
            TTL = MulticastService.HostRecordTTL
        });
        Resources.Add(new TXTRecord
        {
            Name = FullyQualifiedName,
            Strings = { "txtvers=1" },
            TTL = MulticastService.NonHostTTL
        });

        foreach (var address in addresses ?? MulticastService.GetLinkLocalAddresses())
        {
            AddressRecord ar = AddressRecord.Create(HostName, address);
            ar.TTL = MulticastService.HostRecordTTL;
            Resources.Add(ar);
        }
    }

    /// <summary>
    ///   The top level domain (TLD) name of the service.
    /// </summary>
    /// <value>
    ///   Always "local".
    /// </value>
    public DomainName Domain { get; } = "local";

    /// <summary>
    ///   A unique name for the service.
    /// </summary>
    /// <value>
    ///   Typically of the form "_<i>service</i>._tcp".
    /// </value>
    /// <remarks>
    ///   It consists of a pair of DNS labels, following the
    ///   <see href="https://www.ietf.org/rfc/rfc2782.txt">SRV records</see> convention.
    ///   The first label of the pair is an underscore character (_) followed by 
    ///   the <see href="https://tools.ietf.org/html/rfc6335">service name</see>. 
    ///   The second label is either "_tcp" (for application
    ///   protocols that run over TCP) or "_udp" (for all others). 
    /// </remarks>
    public DomainName? ServiceName
    { 
        get => _serviceName; 
        init 
        {
            _serviceName = value;
            if (_instanceName != null && _serviceName != null)
            {
                var simpleServiceName = new DomainName(_serviceName.ToString()
                    .Replace("._tcp", "", StringComparison.Ordinal)
                    .Replace("._udp", "", StringComparison.Ordinal)
                    .Trim('_')
                    .Replace('_', '-'));
                HostName = DomainName.Join(_instanceName, simpleServiceName, Domain);
            }
            DomainName fqn = FullyQualifiedName;
            foreach (var srvRecord in Resources.OfType<SRVRecord>())
                srvRecord.Name = fqn;

            foreach (var txtRecord in Resources.OfType<TXTRecord>())
                txtRecord.Name = fqn;
        }
    }

    /// <summary>
    ///   A unique identifier for the service instance.
    /// </summary>
    /// <value>
    ///   Some unique value.
    /// </value>
    public DomainName? InstanceName
    {
        get => _instanceName;
        init
        {
            _instanceName = value;
            if (_serviceName != null)
            {
                if (_instanceName is null)
                    throw new InvalidOperationException($"{nameof(InstanceName)} cannot be null when {nameof(ServiceName)} has a value.");
                var simpleServiceName = new DomainName(_serviceName.ToString()
                    .Replace("._tcp", "", StringComparison.Ordinal)
                    .Replace("._udp", "", StringComparison.Ordinal)
                    .Trim('_')
                    .Replace('_', '-'));
                HostName = DomainName.Join(_instanceName, simpleServiceName, Domain);
            }
            DomainName fqn = FullyQualifiedName;
            foreach (var srvRecord in Resources.OfType<SRVRecord>())
                srvRecord.Name = fqn;

            foreach (var txtRecord in Resources.OfType<TXTRecord>())
                txtRecord.Name = fqn;
        }
    }

    /// <summary>
    ///   The service name and domain.
    /// </summary>
    /// <value>
    ///   Typically of the form "_<i>service</i>._tcp.local".
    /// </value>
    public DomainName QualifiedServiceName
    {
        get
        {
            if (_serviceName is null)
                throw new InvalidOperationException($"{nameof(ServiceName)} is not set.");
            
            return DomainName.Join(_serviceName, Domain);
        }
    }

    /// <summary>
    ///   The fully qualified name of the instance's host.
    /// </summary>
    /// <remarks>
    ///   This can be used to query the address records (A and AAAA)
    ///   of the service instance.
    /// </remarks>
    public DomainName HostName
    {
        get => _hostName;
        init
        {
            _hostName = value;
            foreach (var srvRecord in Resources.OfType<SRVRecord>())
                srvRecord.Target = _hostName;

            foreach (var addressRecord in Resources.OfType<AddressRecord>())
                addressRecord.Name = _hostName;
        }
    }

    /// <summary>
    ///   The instance name, service name and domain.
    /// </summary>
    /// <value>
    ///   <see cref="InstanceName"/>.<see cref="ServiceName"/>.<see cref="Domain"/>
    /// </value>
    public DomainName FullyQualifiedName
    {
        get
        {
            if (_instanceName is null || _serviceName is null)
                throw new InvalidOperationException($"{nameof(InstanceName)} and {nameof(ServiceName)} must be set.");
            
            return DomainName.Join(_instanceName, _serviceName, Domain);
        }
    }

    /// <summary>
    ///   DNS resource records that are used to locate the service instance.
    /// </summary>
    /// <value>
    ///   More information about the service.
    /// </value>
    /// <remarks>
    ///   All records should have the <see cref="ResourceRecord.Name"/> equal
    ///   to the <see cref="FullyQualifiedName"/> or the <see cref="HostName"/>.
    ///   <para>
    ///   At a minimum the <see cref="SRVRecord"/> and <see cref="TXTRecord"/>
    ///   records must be present.
    ///   Typically <see cref="AddressRecord">address records</see>
    ///   are also present and are associated with <see cref="HostName"/>.
    ///   </para>
    /// </remarks>
    public List<ResourceRecord> Resources { get; set; } = [];

    /// <summary>
    ///   A list of service features implemented by the service instance.
    /// </summary>
    /// <value>
    ///   The default is an empty list.
    /// </value>
    /// <seealso href="https://tools.ietf.org/html/rfc6763#section-7.1"/>
    public List<string> Subtypes { get; set; } = [];

    /// <summary>
    /// If set, this profile is shared between multiple mDNS responders.
    /// This server does not own the profile exclusively.
    /// </summary>
    public bool SharedProfile { get; set; }

    /// <summary>
    ///   Add a property of the service to the <see cref="TXTRecord"/>.
    /// </summary>
    /// <param name="key">
    ///   The name of the property.
    /// </param>
    /// <param name="value">
    ///   The value of the property.
    /// </param>
    public void AddProperty(string key, string value)
    {
        var txt = Resources.OfType<TXTRecord>().FirstOrDefault();
        if (txt == null)
        {
            txt = new TXTRecord { Name = FullyQualifiedName };
            Resources.Add(txt);
        }
        txt.Strings.Add($"{key}={value}");
    }
}