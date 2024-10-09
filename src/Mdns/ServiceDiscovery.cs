using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Makaretu.Dns.Resolving;

using Microsoft.Extensions.Logging;

namespace Makaretu.Dns;

/// <summary>
///   DNS based Service Discovery is a way of using standard DNS programming interfaces, servers,
///   and packet formats to browse the network for services.
/// </summary>
/// <seealso href="https://tools.ietf.org/html/rfc6763">RFC 6763 DNS-Based Service Discovery</seealso>
public class ServiceDiscovery : IServiceDiscovery
{
    private static readonly DomainName LocalDomain = new("local");
    private static readonly DomainName SubName = new("_sub");

    /// <summary>
    ///   The service discovery service name.
    /// </summary>
    /// <value>
    ///   The service name used to enumerate other services.
    /// </value>
    public static readonly DomainName ServiceName = new("_services._dns-sd._udp.local");

    private readonly bool _instantiatedMdns;
    private readonly List<ServiceProfile> _profiles = [];
    private readonly ILogger<ServiceDiscovery>? _logger;

    /// <summary>
    ///  Creates a new instance of the <see cref="ServiceDiscovery"/> class with the specified <see cref="IMulticastService"/>.
    /// </summary>
    /// <param name="mdns">The underlying <see cref="IMulticastService"/> to use.</param>
    /// <param name="loggerFactory">Optional logger factory.</param>
    /// <param name="cancellationToken">The cancellation token used to stop mDNS</param>
    public static async Task<ServiceDiscovery> CreateInstance(IMulticastService? mdns = null, ILoggerFactory? loggerFactory = null, CancellationToken cancellationToken = default)
    {
        ServiceDiscovery instance;
        if (mdns is null)
        {
            instance = new ServiceDiscovery(loggerFactory);
            if (instance.Mdns is not null)
            {
                // Auto start.
                await instance.Mdns.Start(cancellationToken);
            }
        }
        else
        {
            instance = new ServiceDiscovery(mdns, loggerFactory);
        }

        return instance;
    }
    
    /// <summary>
    ///   Creates a new instance of the <see cref="ServiceDiscovery"/> class.
    /// </summary>
    private ServiceDiscovery(ILoggerFactory? loggerFactory)
        : this(new MulticastService(loggerFactory: loggerFactory), loggerFactory) =>
        _instantiatedMdns = true;

    /// <summary>
    ///   Creates a new instance of the <see cref="ServiceDiscovery"/> class with
    ///   the specified <see cref="IMulticastService"/>.
    /// </summary>
    /// <param name="mdns">
    ///   The underlying <see cref="IMulticastService"/> to use.
    /// </param>
    /// <param name="loggerFactory">Optional logger factory</param>
    private ServiceDiscovery(IMulticastService mdns, ILoggerFactory? loggerFactory)
    {
        Mdns = mdns;
        _logger = loggerFactory?.CreateLogger<ServiceDiscovery>();
        mdns.QueryReceived += OnQuery;
        mdns.AnswerReceived += OnAnswer;
    }

    /// <summary>
    ///   Gets the multicasting service.
    /// </summary>
    /// <value>
    ///   Is used to send and receive multicast <see cref="Message">DNS messages</see>.
    /// </value>
    public IMulticastService? Mdns { get; private set; }

    /// <summary>
    ///   Add the additional records into the answers.
    /// </summary>
    /// <value>
    ///   Defaults to <b>false</b>.
    /// </value>
    /// <remarks>
    ///   Some malformed systems, such as js-ipfs and go-ipfs, only examine
    ///   the <see cref="Message.Answers"/> and not the <see cref="Message.AdditionalRecords"/>.
    ///   Setting this to <b>true</b>, will move the additional records
    ///   into the answers.
    ///   <para>
    ///   This never done for DNS-SD answers.
    ///   </para>
    /// </remarks>
    public bool AnswersContainsAdditionalRecords { get; set; }

    /// <summary>
    ///   Gets the name server.
    /// </summary>
    /// <value>
    ///   Is used to answer questions.
    /// </value>
    public NameServer NameServer { get; } = new()
    {
        Catalog = new Catalog(),
        AnswerAllQuestions = true
    };

    /// <summary>
    ///   Raised when a DNS-SD response is received.
    /// </summary>
    /// <value>
    ///   Contains the service name.
    /// </value>
    /// <remarks>
    ///   <b>ServiceDiscovery</b> passively monitors the network for any answers
    ///   to a DNS-SD query. When an answer is received this event is raised.
    ///   <para>
    ///   Use <see cref="QueryAllServices"/> to initiate a DNS-SD question.
    ///   </para>
    /// </remarks>
    public Func<DomainName, Task>? ServiceDiscovered { get; set; }

    /// <summary>
    ///   Raised when a service instance is discovered.
    /// </summary>
    /// <value>
    ///   Contains the service instance name.
    /// </value>
    /// <remarks>
    ///   <b>ServiceDiscovery</b> passively monitors the network for any answers.
    ///   When an answer containing a PTR to a service instance is received
    ///   this event is raised.
    /// </remarks>
    public Func<ServiceInstanceDiscoveryEventArgs, Task>? ServiceInstanceDiscovered { get; set; }

    /// <summary>
    ///   Raised when a service instance is shutting down.
    /// </summary>
    /// <value>
    ///   Contains the service instance name.
    /// </value>
    /// <remarks>
    ///   <b>ServiceDiscovery</b> passively monitors the network for any answers.
    ///   When an answer containing a PTR to a service instance with a
    ///   TTL of zero is received this event is raised.
    /// </remarks>
    public Func<ServiceInstanceShutdownEventArgs, Task>? ServiceInstanceShutdown { get; set; }

    /// <summary>
    ///    Asks other MDNS services to send their service names.
    /// </summary>
    /// <remarks>
    ///   When an answer is received the <see cref="ServiceDiscovered"/> event is raised.
    /// </remarks>
    public async Task QueryAllServices()
    {
        if (Mdns is not null)
            await Mdns.SendQuery(ServiceName, type: DnsType.PTR);
    }

    /// <summary>
    ///    Asks other MDNS services to send their service names;
    ///    accepts unicast and/or broadcast answers.
    /// </summary>
    /// <remarks>
    ///   When an answer is received the <see cref="ServiceDiscovered"/> event is raised.
    /// </remarks>
    public async Task QueryUnicastAllServices()
    {
        if (Mdns is not null)
            await Mdns.SendUnicastQuery(ServiceName, type: DnsType.PTR);
    }

    /// <summary>
    ///   Asks instances of the specified service to send details.
    /// </summary>
    /// <param name="service">
    ///   The service name to query. Typically of the form "_<i>service</i>._tcp".
    /// </param>
    /// <remarks>
    ///   When an answer is received the <see cref="ServiceInstanceDiscovered"/> event is raised.
    /// </remarks>
    /// <seealso cref="ServiceProfile.ServiceName"/>
    public async Task QueryServiceInstances(DomainName service)
    {
        if (Mdns is not null)
            await Mdns.SendQuery(DomainName.Join(service, LocalDomain), type: DnsType.PTR);
    }

    /// <summary>
    ///   Asks instances of the specified service with the subtype to send details.
    /// </summary>
    /// <param name="service">
    ///   The service name to query. Typically of the form "_<i>service</i>._tcp".
    /// </param>
    /// <param name="subtype">
    ///   The feature that is needed.
    /// </param>
    /// <remarks>
    ///   When an answer is received the <see cref="ServiceInstanceDiscovered"/> event is raised.
    /// </remarks>
    /// <seealso cref="ServiceProfile.ServiceName"/>
    public async Task QueryServiceInstances(DomainName service, string subtype)
    {
        if (Mdns is null)
            return;
        
        var name = DomainName.Join(
            new DomainName(subtype),
            SubName,
            service,
            LocalDomain);
        await Mdns.SendQuery(name, type: DnsType.PTR);
    }

    /// <summary>
    ///   Asks instances of the specified service to send details.
    ///   accepts unicast and/or broadcast answers.
    /// </summary>
    /// <param name="service">
    ///   The service name to query. Typically of the form "_<i>service</i>._tcp".
    /// </param>
    /// <remarks>
    ///   When an answer is received the <see cref="ServiceInstanceDiscovered"/> event is raised.
    /// </remarks>
    /// <seealso cref="ServiceProfile.ServiceName"/>
    public async Task QueryUnicastServiceInstances(DomainName service)
    {
        if (Mdns is not null)
            await Mdns.SendUnicastQuery(DomainName.Join(service, LocalDomain), type: DnsType.PTR);
    }

    /// <summary>
    ///   Advertise a service profile.
    /// </summary>
    /// <param name="service">
    ///   The service profile.
    /// </param>
    /// <remarks>
    ///   Any queries for the service or service instance will be answered with
    ///   information from the profile.
    ///   <para>
    ///   Besides adding the profile's resource records to the <see cref="Catalog"/> PTR records are
    ///   created to support DNS-SD and reverse address mapping (DNS address lookup).
    ///   </para>
    /// </remarks>
    public void Advertise(ServiceProfile service)
    {
        _profiles.Add(service);

        var catalog = NameServer.Catalog;
        catalog?.Add(
            new PTRRecord { Name = ServiceName, DomainName = service.QualifiedServiceName },
            authoritative: true);
        catalog?.Add(
            new PTRRecord { Name = service.QualifiedServiceName, DomainName = service.FullyQualifiedName },
            authoritative: true);

        foreach (var subtype in service.Subtypes)
        {
            var ptr = new PTRRecord
            {
                Name = DomainName.Join(
                    new DomainName(subtype),
                    SubName,
                    service.QualifiedServiceName),
                DomainName = service.FullyQualifiedName
            };
            catalog?.Add(ptr, authoritative: true);
        }

        foreach (var r in service.Resources)
        {
            catalog?.Add(r, authoritative: true);
        }

        catalog?.IncludeReverseLookupRecords();
    }

    /// <summary>
    /// Probe the network to ensure the service is unique. Shared profiles should skip this step.
    /// </summary>
    /// <param name="profile"></param>
    /// <returns>True if this service conflicts with an existing network service</returns>
    /// <exception cref="InvalidOperationException">Thrown if a shared profile is probed</exception>
    public async Task<bool> Probe(ServiceProfile profile)
    {
        if (profile.SharedProfile)
            throw new InvalidOperationException("Shared profiles should not be probed");

        bool conflict = false;

        if (Mdns is null)
            return conflict;
        
        Mdns.AnswerReceived += Handler;

        await Task.Delay(Random.Shared.Next(0, 250));
        await Mdns.SendUnicastQuery(profile.HostName);
        await Task.Delay(250);
        if (!conflict)
        {
            await Mdns.SendUnicastQuery(profile.HostName);
            await Task.Delay(250);
            if (!conflict)
            {
                await Mdns.SendUnicastQuery(profile.HostName);
                await Task.Delay(250);
            }
        }
        Mdns.AnswerReceived -= Handler;
        return conflict;

        // Convert handler to task
        Task Handler(MessageEventArgs e)
        {
            foreach (ResourceRecord answer in e.Message.Answers)
            {
                if ((DnsClass)((ushort)answer.Class & ~MulticastService.CacheFlushBit) == DnsClass.IN && answer.Name?.Equals(profile.HostName) == true)
                {
                    conflict = true;
                    return Task.CompletedTask;
                }
            }
            
            return Task.CompletedTask;
        }
    }

    /// <summary>
    ///    Sends an unsolicited MDNS response describing the
    ///    service profile.
    /// </summary>
    /// <param name="profile">
    ///   The profile to describe.
    /// </param>
    /// <param name="numberOfTimes">
    ///     How many times to announce this service profile. Range [2 - 8]
    /// </param>
    /// <remarks>
    ///   Sends a MDNS response <see cref="Message"/> containing the pointer
    ///   and resource records of the <paramref name="profile"/>.
    ///   <para>
    ///   To provide increased robustness against packet loss,
    ///   two unsolicited responses are sent one second apart.
    ///   </para>
    /// </remarks>
    public async Task Announce(ServiceProfile profile, int numberOfTimes = 2)
    {
        numberOfTimes = Math.Max(Math.Min(numberOfTimes, 8), 2);
        var message = new Message { QR = true };

        // Add the shared records.
        var ptrRecord = new PTRRecord { Name = profile.QualifiedServiceName, DomainName = profile.FullyQualifiedName };
        ptrRecord.Class = (DnsClass)((ushort)ptrRecord.Class);
        message.Answers.Add(ptrRecord);

        // Add the resource records.
        profile.Resources.ForEach(resource =>
        {
            var newResource = resource.Clone() as ResourceRecord;
            if (!profile.SharedProfile && newResource != null)
            {
                newResource.Class = (DnsClass)((ushort)newResource.Class | MulticastService.CacheFlushBit);
            }

            if (newResource is not null)
                message.Answers.Add(newResource);
        });

        if (Mdns is null)
            return;
        
        for (int i = 0; i < numberOfTimes; i++)
        {
            if (i > 0)
                await Task.Delay(501 * (1 << i));
            await Mdns.SendAnswer(message, false);
        }
    }

    /// <summary>
    /// Sends a goodbye message for each announced service
    /// and removes its pointer from the name sever.
    /// </summary>
    public async Task Unadvertise()
    {
        foreach (ServiceProfile serviceProfile in _profiles)
        {
            await Unadvertise(serviceProfile);
        }
    }

    /// <summary>
    /// Sends a goodbye message for the provided
    /// profile and removes its pointer from the name sever.
    /// </summary>
    /// <param name="profile">The profile to send a goodbye message for.</param>
    public async Task Unadvertise(ServiceProfile profile)
    {
        var message = new Message { QR = true };
        var ptrRecord = new PTRRecord { Name = profile.QualifiedServiceName, DomainName = profile.FullyQualifiedName, TTL = TimeSpan.Zero };

        message.Answers.Add(ptrRecord);
        profile.Resources.ForEach(resource =>
        {
            resource.TTL = TimeSpan.Zero;
            message.AdditionalRecords.Add(resource);
        });

        if (Mdns is not null)
            await Mdns.SendAnswer(message);

        NameServer.Catalog?.TryRemove(profile.QualifiedServiceName, out Node? _);
    }

    private async Task OnAnswer(MessageEventArgs e)
    {
        var msg = e.Message;
        if (_logger?.IsEnabled(LogLevel.Debug) is true)
            _logger?.LogDebug("Answer from {RemoteEndPoint}", e.RemoteEndPoint);
            
        if (_logger?.IsEnabled(LogLevel.Trace) is true)
            _logger.LogTrace("{@Message}", msg);

        // Any DNS-SD answers?
        var sd = msg.Answers
            .OfType<PTRRecord>()
            .Where(static ptr => ptr.Name?.IsSubdomainOf(LocalDomain) is true);
        foreach (var ptr in sd)
        {
            if (ptr.Name == ServiceName)
            {
                if (ServiceDiscovered is not null && ptr.DomainName is not null)
                    await ServiceDiscovered(ptr.DomainName);
            }
            else if (ptr.TTL == TimeSpan.Zero)
            {
                if (ServiceInstanceShutdown is not null)
                {
                    var args = new ServiceInstanceShutdownEventArgs
                    {
                        ServiceInstanceName = ptr.DomainName,
                        Message = msg,
                        RemoteEndPoint = e.RemoteEndPoint
                    };
                    await ServiceInstanceShutdown(args);
                }
            }
            else
            {
                if (ServiceInstanceDiscovered is not null)
                {
                    var args = new ServiceInstanceDiscoveryEventArgs
                    {
                        ServiceInstanceName = ptr.DomainName,
                        Message = msg,
                        RemoteEndPoint = e.RemoteEndPoint
                    };
                    await ServiceInstanceDiscovered(args);
                }
            }
        }
    }

    private async Task OnQuery(MessageEventArgs e)
    {
        var request = e.Message;

        if (_logger?.IsEnabled(LogLevel.Debug) is true)
            _logger.LogDebug("Query from {RemoteEndPoint}", e.RemoteEndPoint);
            
        if (_logger?.IsEnabled(LogLevel.Trace) is true)
            _logger.LogTrace("{@Message}", request);

        // Determine if this query is requesting a unicast response
        // and normalise the Class.
        var QU = false; // unicast query response?
        foreach (var r in request.Questions)
        {
            if (((ushort)r.Class & MulticastService.UnicastResponseBit) != 0)
            {
                QU = true;
                r.Class = (DnsClass)((ushort)r.Class & ~MulticastService.UnicastResponseBit);
            }
        }

        var response = await NameServer.ResolveAsync(request);

        if (response.Status != MessageStatus.NoError)
        {
            return;
        }

        // Many bonjour browsers don't like DNS-SD response
        // with additional records.
        if (response.Answers.Exists(static a => a.Name == ServiceName))
        {
            response.AdditionalRecords.Clear();
        }

        if (AnswersContainsAdditionalRecords)
        {
            response.Answers.AddRange(response.AdditionalRecords);
            response.AdditionalRecords.Clear();
        }

        if (QU && MulticastService.EnableUnicastAnswers)
        {
            if (Mdns is not null)
                await Mdns.SendAnswer(response, e, false, e.RemoteEndPoint); //Send a unicast response
        }
        else if (Mdns is not null)
        {
            await Mdns.SendAnswer(response, e, !QU);
        }

        _logger?.LogDebug("Sending answer");
        if (_logger?.IsEnabled(LogLevel.Trace) is true)
        {
            _logger?.LogTrace("{@Message}", response);
        }
    }

    #region IDisposable Support

    /// <summary>
    /// Dispose of the <see cref="ServiceDiscovery"/> instance.
    /// </summary>
    /// <param name="disposing"></param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (Mdns != null)
            {
                Mdns.QueryReceived -= OnQuery;
                Mdns.AnswerReceived -= OnAnswer;
                if (_instantiatedMdns)
                {
                    Mdns.Dispose();
                }
                Mdns = null;
            }
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    #endregion IDisposable Support
}