using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

namespace Makaretu.Dns;

/// <summary>
///   Muticast Domain Name Service.
/// </summary>
/// <remarks>
///   Sends and receives DNS queries and answers via the multicast mechanism
///   defined in <see href="https://tools.ietf.org/html/rfc6762"/>.
///   <para>
///   Use <see cref="Start"/> to start listening for multicast messages.
///   One of the events, <see cref="QueryReceived"/> or <see cref="AnswerReceived"/>, is
///   raised when a <see cref="Message"/> is received.
///   </para>
/// </remarks>
public class MulticastService : IMulticastService
{
    // IP header (20 bytes for IPv4; 40 bytes for IPv6) and the UDP header(8 bytes).
    private const int PacketOverhead = 48;
    private const int MaxDatagramSize = Message.MaxLength;

    private static readonly TimeSpan MaxLegacyUnicastTtl = TimeSpan.FromSeconds(10);

    private List<NetworkInterface> _knownNics = [];
    private readonly ILogger<MulticastService>? _logger;
    private int _maxPacketSize;

    /// <summary>
    /// When this bit is set in a question, it indicates that the querier is willing to accept unicast replies in response to this specific query,
    /// as well as the usual multicast responses.
    /// </summary>
    public const int UnicastResponseBit = 0x8000;
    
    /// <summary>
    /// If the record is one that has been verified unique, the host sets the most significant bit of the rrclass field of the resource record.
    /// This bit, the cache-flush bit, tells neighboring hosts that this is not a shared record type.
    /// </summary>
    public const int CacheFlushBit = 0x8000;
    
    /// <summary>
    ///   Recently sent messages.
    /// </summary>
    private readonly RecentMessages _sentMessages = new();

    /// <summary>
    ///   Recently received messages.
    /// </summary>
    private readonly RecentMessages _receivedMessages = new();

    /// <summary>
    ///   The multicast client.
    /// </summary>
    private MulticastClient? _client;

    /// <summary>
    ///   Use to send unicast IPv4 answers.
    /// </summary>
    private readonly UdpClient? _unicastClientIp4;

    /// <summary>
    ///   Use to send unicast IPv6 answers.
    /// </summary>
    private readonly UdpClient? _unicastClientIp6;

    /// <summary>
    ///   Function used for listening filtered network interfaces.
    /// </summary>
    private readonly Func<IEnumerable<NetworkInterface>, IEnumerable<NetworkInterface>>? _networkInterfacesFilter;

    private readonly ILoggerFactory? _loggerFactory;

    /// <summary>
    ///   Raised when any local MDNS service sends a query.
    /// </summary>
    /// <value>
    ///   Contains the query <see cref="Message"/>.
    /// </value>
    /// <remarks>
    ///   Any exception throw by the event handler is simply logged and
    ///   then forgotten.
    /// </remarks>
    /// <seealso cref="SendQuery(Message)"/>
    public Func<MessageEventArgs, Task>? QueryReceived { get; set; }

    /// <summary>
    ///   Raised when any link-local MDNS service responds to a query.
    /// </summary>
    /// <value>
    ///   Contains the answer <see cref="Message"/>.
    /// </value>
    /// <remarks>
    ///   Any exception throw by the event handler is simply logged and
    ///   then forgotten.
    /// </remarks>
    public Func<MessageEventArgs, Task>? AnswerReceived { get; set; }

    /// <summary>
    ///   Raised when a DNS message is received that cannot be decoded.
    /// </summary>
    /// <value>
    ///   The DNS message as a byte array.
    /// </value>
    public Func<byte[], Task>? MalformedMessage { get; set; }

    /// <summary>
    ///   Raised when one or more network interfaces are discovered.
    /// </summary>
    /// <value>
    ///   Contains the network interface(s).
    /// </value>
    public Func<NetworkInterfaceEventArgs, Task>? NetworkInterfaceDiscovered { get; set; }

    /// <summary>
    ///   Create a new instance of the <see cref="MulticastService"/> class.
    /// </summary>
    /// <param name="filter">
    ///   Multicast listener will be bound to result of filtering function.
    /// </param>
    /// <param name="loggerFactory">Optional logger factory.</param>
    public MulticastService(Func<IEnumerable<NetworkInterface>, IEnumerable<NetworkInterface>>? filter = null,
        ILoggerFactory? loggerFactory = null)
    {
        _logger = _loggerFactory?.CreateLogger<MulticastService>();
        _networkInterfacesFilter = filter;
        _loggerFactory = loggerFactory;

        UseIpv4 = Socket.OSSupportsIPv4;
        if (UseIpv4)
            _unicastClientIp4 = new UdpClient(AddressFamily.InterNetwork);
        UseIpv6 = Socket.OSSupportsIPv6;
        if (UseIpv6)
            _unicastClientIp6 = new UdpClient(AddressFamily.InterNetworkV6);
        IgnoreDuplicateMessages = true;
    }

    /// <summary>
    ///   Send and receive on IPv4.
    /// </summary>
    /// <value>
    ///   Defaults to <b>true</b> if the OS supports it.
    /// </value>
    public bool UseIpv4 { get; set; }

    /// <summary>
    ///   Send and receive on IPv6.
    /// </summary>
    /// <value>
    ///   Defaults to <b>true</b> if the OS supports it.
    /// </value>
    public bool UseIpv6 { get; set; }

    /// <summary>
    ///   Determines if received messages are checked for duplicates.
    /// </summary>
    /// <value>
    ///   <b>true</b> to ignore duplicate messages. Defaults to <b>true</b>.
    /// </value>
    /// <remarks>
    ///   When set, a message that has been received within the last second
    ///   will be ignored.
    /// </remarks>
    public bool IgnoreDuplicateMessages { get; set; }

    /// <summary>
    /// Determines whether loopback interfaces should be excluded when other network interfaces are available
    /// </summary>
    /// <value>
    /// <b>true</b> to always include loopback interfaces.
    /// <b>false</b> to only include loopback interfaces when no other interfaces exist.
    /// Defaults to <b>false</b>.
    /// </value>
    public static bool IncludeLoopbackInterfaces { get; set; }

    /// <summary>
    /// Allow answering queries in unicast. When multiple services are sharing a port this should be set to false, otherwise true.
    /// </summary>
    /// <b>true</b> to respond to unicast queries with unicast responses.
    /// <b>false</b> to always answer queries with unicast.
    /// Defaults to <b>true</b>.
    public static bool EnableUnicastAnswers { get; set; } = true;

    /// <summary>
    /// Per https://tools.ietf.org/html/rfc6762 section 10: All records containing
    /// Host in the record OR Rdata should have a default TTL of 2 mins
    /// </summary>
    public static readonly TimeSpan HostRecordTTL = TimeSpan.FromSeconds(120);
    /// <summary>
    /// Per https://tools.ietf.org/html/rfc6762 section 10:
    /// All records NOT containing Host in the record OR Rdata should have a default TTL of 75 mins
    /// </summary>
    public static readonly TimeSpan NonHostTTL = TimeSpan.FromMinutes(75);

    /// <summary>
    ///   Get the network interfaces that are useable.
    /// </summary>
    /// <returns>
    ///   A sequence of <see cref="NetworkInterface"/>.
    /// </returns>
    /// <remarks>
    ///   The following filters are applied
    ///   <list type="bullet">
    ///   <item><description>interface is enabled</description></item>
    ///   <item><description>interface is not a loopback</description></item>
    ///   </list>
    ///   <para>
    ///   If no network interface is operational, then the loopback interface(s)
    ///   are included (127.0.0.1 and/or ::1).
    ///   </para>
    /// </remarks>
    public static IEnumerable<NetworkInterface> GetNetworkInterfaces()
    {
        var nics = NetworkInterface.GetAllNetworkInterfaces()
            .Where(static nic => nic is { OperationalStatus: OperationalStatus.Up, IsReceiveOnly: false, SupportsMulticast: true })
            .Where(static nic => IncludeLoopbackInterfaces || (nic.NetworkInterfaceType != NetworkInterfaceType.Loopback))
            .ToArray();
        if (nics.Length > 0)
            return nics;

        // Special case: no operational NIC, then use loopbacks.
        return NetworkInterface.GetAllNetworkInterfaces()
            .Where(static nic => nic is { OperationalStatus: OperationalStatus.Up, IsReceiveOnly: false, SupportsMulticast: true });
    }

    /// <summary>
    ///   Get the IP addresses of the local machine.
    /// </summary>
    /// <returns>
    ///   A sequence of IP addresses of the local machine.
    /// </returns>
    /// <remarks>
    ///   The loopback addresses (127.0.0.1 and ::1) are NOT included in the
    ///   returned sequences.
    /// </remarks>
    public static IEnumerable<IPAddress> GetIPAddresses()
    {
        return GetNetworkInterfaces()
            .SelectMany(static nic => nic.GetIPProperties().UnicastAddresses)
            .Select(static u => u.Address);
    }

    /// <summary>
    ///   Get the link local IP addresses of the local machine.
    /// </summary>
    /// <returns>
    ///   A sequence of IP addresses.
    /// </returns>
    /// <remarks>
    ///   All IPv4 addresses are considered link local.
    /// </remarks>
    /// <seealso href="https://en.wikipedia.org/wiki/Link-local_address"/>
    public static IEnumerable<IPAddress> GetLinkLocalAddresses()
    {
        return GetIPAddresses()
            .Where(static a => a.AddressFamily == AddressFamily.InterNetwork ||
                               a is { AddressFamily: AddressFamily.InterNetworkV6, IsIPv6LinkLocal: true });
    }

    /// <summary>
    ///   Start the service.
    /// </summary>
    public async Task Start(CancellationToken cancellationToken)
    {
        _maxPacketSize = MaxDatagramSize - PacketOverhead;

        _knownNics.Clear();

        await FindNetworkInterfaces(cancellationToken);
    }

    /// <summary>
    ///   Stop the service.
    /// </summary>
    /// <remarks>
    ///   Clears all the event handlers.
    /// </remarks>
    public void Stop()
    {
        // All event handlers are cleared.
        QueryReceived = null;
        AnswerReceived = null;
        NetworkInterfaceDiscovered = null;
        if (OperatingSystem.IsWindows())
        {
            NetworkChange.NetworkAddressChanged -= OnNetworkAddressChanged;
        }
        // Stop current UDP listener
        _client?.Dispose();
        _client = null;
    }

    private void OnNetworkAddressChanged(object? sender, EventArgs e) => _ = FindNetworkInterfaces(CancellationToken.None);

    private async Task FindNetworkInterfaces(CancellationToken cancellationToken)
    {
        _logger?.LogDebug("Finding network interfaces");

        try
        {
            var currentNics = GetNetworkInterfaces().ToList();

            var newNics = new List<NetworkInterface>();
            var oldNics = new List<NetworkInterface>();

            foreach (var nic in _knownNics.Where(k => currentNics.TrueForAll(n => k.Id.Equals(n.Id, StringComparison.Ordinal))))
            {
                oldNics.Add(nic);

                if (_logger?.IsEnabled(LogLevel.Debug) is true)
                {
                    _logger.LogDebug("Removed nic '{NicName}'.", nic.Name);
                }
            }

            foreach (var nic in currentNics.Where(nic => _knownNics.TrueForAll(k => k.Id.Equals(nic.Id, StringComparison.Ordinal))))
            {
                newNics.Add(nic);

                if (_logger?.IsEnabled(LogLevel.Debug) is true)
                {
                    _logger.LogDebug("Found nic '{NicName}'.", nic.Name);
                }
            }

            _knownNics = currentNics;

            // Only create client if something has changed.
            if (newNics.Count > 0 || oldNics.Count > 0)
            {
                _client?.Dispose();
                _client = new MulticastClient(UseIpv4, UseIpv6, _networkInterfacesFilter?.Invoke(_knownNics) ?? _knownNics, cancellationToken, _loggerFactory?.CreateLogger<MulticastClient>());
                _client.MessageReceived += OnDnsMessage;
            }

            // Tell others.
            if (newNics.Count > 0 && NetworkInterfaceDiscovered is not null)
            {
                await NetworkInterfaceDiscovered(new NetworkInterfaceEventArgs
                {
                    NetworkInterfaces = newNics
                });
            }

            // Magic from @eshvatskyi
            //
            // I've seen situation when NetworkAddressChanged is not triggered
            // (wifi off, but NIC is not disabled, wifi - on, NIC was not changed
            // so no event). Rebinding fixes this.
            //
            // Do magic only on Windows.
            if (OperatingSystem.IsWindows())
            {
                NetworkChange.NetworkAddressChanged -= OnNetworkAddressChanged;
                NetworkChange.NetworkAddressChanged += OnNetworkAddressChanged;
            }
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "FindNics failed");
        }
    }

    /// <inheritdoc />
    public async Task<Message> ResolveAsync(Message request, CancellationToken cancel = default)
    {
        var tsc = new TaskCompletionSource<Message>();

        cancel.Register(() =>
        {
            AnswerReceived -= CheckResponse;
            tsc.TrySetCanceled(cancel);
        });

        AnswerReceived += CheckResponse;
        await SendQuery(request);

        return await tsc.Task;
        
        Task CheckResponse(MessageEventArgs e)
        {
            var response = e.Message;
            if (request.Questions.TrueForAll(q => response.Answers.Exists(a => a.Name == q.Name)))
            {
                AnswerReceived -= CheckResponse;
                tsc.SetResult(response);
            }

            return Task.CompletedTask;
        }
    }

    /// <summary>
    ///   Ask for answers about a name.
    /// </summary>
    /// <param name="name">
    ///   A domain name that should end with ".local", e.g. "myservice.local".
    /// </param>
    /// <param name="class">
    ///   The class, defaults to <see cref="DnsClass.IN"/>.
    /// </param>
    /// <param name="type">
    ///   The question type, defaults to <see cref="DnsType.ANY"/>.
    /// </param>
    /// <remarks>
    ///   Answers to any query are obtained on the <see cref="AnswerReceived"/>
    ///   event.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    ///   When the service has not started.
    /// </exception>
    public async Task SendQuery(DomainName name, DnsClass @class = DnsClass.IN, DnsType type = DnsType.ANY)
    {
        var msg = new Message
        {
            Opcode = MessageOperation.Query,
            QR = false
        };
        msg.Questions.Add(new Question
        {
            Name = name,
            Class = @class,
            Type = type
        });

        await SendQuery(msg);
    }

    /// <summary>
    ///   Ask for answers about a name and accept unicast and/or broadcast response.
    /// </summary>
    /// <param name="name">
    ///   A domain name that should end with ".local", e.g. "myservice.local".
    /// </param>
    /// <param name="class">
    ///   The class, defaults to <see cref="DnsClass.IN"/>.
    /// </param>
    /// <param name="type">
    ///   The question type, defaults to <see cref="DnsType.ANY"/>.
    /// </param>
    /// <remarks>
    ///   Send a "QU" question (unicast). The most significant bit of the Class is set.
    ///   Answers to any query are obtained on the <see cref="AnswerReceived"/>
    ///   event.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    ///   When the service has not started.
    /// </exception>
    public async Task SendUnicastQuery(DomainName name, DnsClass @class = DnsClass.IN, DnsType type = DnsType.ANY)
    {
        var msg = new Message
        {
            Opcode = MessageOperation.Query,
            QR = false
        };
        msg.Questions.Add(new Question
        {
            Name = name,
            Class = (DnsClass)((ushort)@class | UnicastResponseBit),
            Type = type
        });

        await SendQuery(msg);
    }

    /// <summary>
    ///   Ask for answers.
    /// </summary>
    /// <param name="msg">
    ///   A query message.
    /// </param>
    /// <remarks>
    ///   Answers to any query are obtained on the <see cref="AnswerReceived"/>
    ///   event.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    ///   When the service has not started.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///   When the serialized <paramref name="msg"/> is too large.
    /// </exception>
    public async Task SendQuery(Message msg)
    {
        UpdateTTL(msg, false);
        await SendAsync(msg, false);
    }

    /// <summary>
    ///   Send an answer to a query.
    /// </summary>
    /// <param name="answer">
    ///   The answer message.
    /// </param>
    /// <param name="checkDuplicate">
    ///   If <b>true</b>, then if the same <paramref name="answer"/> was
    ///   recently sent it will not be sent again.
    /// </param>
    /// <param name="unicastEndpoint">
    ///     If defined, will generate a unicast response to the provided endpoint
    /// </param>
    /// <exception cref="InvalidOperationException">
    ///   When the service has not started.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///   When the serialized <paramref name="answer"/> is too large.
    /// </exception>
    /// <remarks>
    ///   <para>
    ///   The <see cref="Message.AA"/> flag is set to true,
    ///   the <see cref="Message.Id"/> set to zero and any questions are removed.
    ///   </para>
    ///   <para>
    ///   The <paramref name="answer"/> is <see cref="Message.Truncate">truncated</see>
    ///   if exceeds the maximum packet length.
    ///   </para>
    ///   <para>
    ///   <paramref name="checkDuplicate"/> should always be <b>true</b> except
    ///   when <see href="https://tools.ietf.org/html/rfc6762#section-8.1">answering a probe</see>.
    ///   </para>
    ///   <note type="caution">
    ///   If possible the <see cref="SendAnswer(Message, MessageEventArgs, bool, IPEndPoint)"/>
    ///   method should be used, so that legacy unicast queries are supported.
    ///   </note>
    /// </remarks>
    /// <see cref="QueryReceived"/>
    /// <seealso cref="Message.CreateResponse"/>
    public async Task SendAnswer(Message answer, bool checkDuplicate = true, IPEndPoint? unicastEndpoint = null)
    {
        // All MDNS answers are authoritative and have a transaction
        // ID of zero.
        answer.AA = true;
        answer.Id = 0;
        answer.Opcode = MessageOperation.Query;
        answer.RA = false;
        answer.AD = false;
        answer.CD = false;

        // All MDNS answers must not contain any questions.
        answer.Questions.Clear();

        answer.Truncate(_maxPacketSize);

        UpdateTTL(answer, false);
        await SendAsync(answer, checkDuplicate, unicastEndpoint);
    }

    /// <summary>
    ///   Send an answer to a query.
    /// </summary>
    /// <param name="answer">
    ///   The answer message.
    /// </param>
    /// <param name="query">
    ///   The query that is being answered.
    /// </param>
    /// <param name="checkDuplicate">
    ///   If <b>true</b>, then if the same <paramref name="answer"/> was
    ///   recently sent it will not be sent again.
    /// </param>
    /// <param name="endPoint">
    ///     The endpoint to send data (unicast) or null (multicast)
    /// </param>
    /// <exception cref="InvalidOperationException">
    ///   When the service has not started.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///   When the serialized <paramref name="answer"/> is too large.
    /// </exception>
    /// <remarks>
    ///   <para>
    ///   If the <paramref name="query"/> is a standard multicast query (sent to port 5353), then
    ///   <see cref="SendAnswer(Message, bool, IPEndPoint)"/> is called.
    ///   </para>
    ///   <para>
    ///   Otherwise a legacy unicast response is sent to sender's end point.
    ///   The <see cref="Message.AA"/> flag is set to true,
    ///   the <see cref="Message.Id"/> is set to query's ID,
    ///   the <see cref="Message.Questions"/> is set to the query's questions,
    ///   and all resource record TTLs have a max value of 10 seconds.
    ///   </para>
    ///   <para>
    ///   The <paramref name="answer"/> is <see cref="Message.Truncate">truncated</see>
    ///   if exceeds the maximum packet length.
    ///   </para>
    ///   <para>
    ///   <paramref name="checkDuplicate"/> should always be <b>true</b> except
    ///   when <see href="https://tools.ietf.org/html/rfc6762#section-8.1">answering a probe</see>.
    ///   </para>
    /// </remarks>
    public async Task SendAnswer(Message answer, MessageEventArgs query, bool checkDuplicate = true, IPEndPoint? endPoint = null)
    {
        if (!query.IsLegacyUnicast)
        {
            await SendAnswer(answer, checkDuplicate, endPoint);
            return;
        }

        answer.AA = true;
        answer.Id = query.Message.Id;
        answer.Questions.Clear();
        answer.Questions.AddRange(query.Message.Questions);
        answer.Truncate(_maxPacketSize);

        UpdateTTL(answer, true);

        await SendAsync(answer, checkDuplicate, query.RemoteEndPoint);
    }

    private async Task SendAsync(Message msg, bool checkDuplicate, IPEndPoint? remoteEndPoint = null)
    {
        var packet = msg.ToByteArray();
        if (packet.Length > _maxPacketSize)
        {
            throw new ArgumentOutOfRangeException($"Exceeds max packet size of {_maxPacketSize}.");
        }

        if (checkDuplicate && !_sentMessages.TryAdd(packet))
        {
            return;
        }

        if (remoteEndPoint == null)
        {
            // Standard multicast response
            if (_client != null)
            {
                await _client.SendAsync(packet);
            }
        }
        // Unicast response
        else
        {
            var unicastClient = (remoteEndPoint.Address.AddressFamily == AddressFamily.InterNetwork)
                ? _unicastClientIp4 : _unicastClientIp6;
            
            if (unicastClient != null)
                await unicastClient.SendAsync(packet, packet.Length, remoteEndPoint);
        }
    }

    private static void UpdateTTL(Message msg, bool legacy)
    {
        foreach (var r in msg.Answers)
            UpdateRecord(r, legacy);

        foreach (var r in msg.AdditionalRecords)
            UpdateRecord(r, legacy);

        foreach (var r in msg.AuthorityRecords)
            UpdateRecord(r, legacy);
    }

    private static void UpdateRecord(ResourceRecord record, bool legacy)
    {
        switch (record.Type)
        {
            case DnsType.A:
            case DnsType.AAAA:
            case DnsType.SRV:
            case DnsType.HINFO:
            case DnsType.PTR:
                if (record.TTL != TimeSpan.Zero)
                    record.TTL = HostRecordTTL;
                break;
            default:
                if (record.TTL != TimeSpan.Zero)
                    record.TTL = NonHostTTL;
                break;
        }
        
        if (legacy && record.TTL > MaxLegacyUnicastTtl)
            record.TTL = MaxLegacyUnicastTtl;
    }

    /// <summary>
    ///   Called by the MulticastClient when a DNS message is received.
    /// </summary>
    /// <param name="result">
    ///   The received message <see cref="UdpReceiveResult"/>.
    /// </param>
    /// <remarks>
    ///   Decodes the <paramref name="result"/> and then raises
    ///   either the <see cref="QueryReceived"/> or <see cref="AnswerReceived"/> event.
    ///   <para>
    ///   Multicast DNS messages received with an OPCODE or RCODE other than zero
    ///   are silently ignored.
    ///   </para>
    ///   <para>
    ///   If the message cannot be decoded, then the <see cref="MalformedMessage"/>
    ///   event is raised.
    ///   </para>
    /// </remarks>
    public async Task OnDnsMessage(UdpReceiveResult result)
    {
        // If recently received, then ignore.
        if (IgnoreDuplicateMessages && !_receivedMessages.TryAdd(result.Buffer))
        {
            return;
        }

        var msg = new Message();
        try
        {
            msg.Read(result.Buffer, 0, result.Buffer.Length);
        }
        catch (Exception e)
        {
            _logger?.LogWarning(e, "Received malformed message");
            if (MalformedMessage is not null)
                await MalformedMessage(result.Buffer);
            
            return; // eat the exception
        }

        //Section 18.3 An opcode other than 0 must be silently ignored
        if (msg.Opcode != MessageOperation.Query || msg.Status != MessageStatus.NoError)
        {
            return;
        }

        // Dispatch the message.
        try
        {
            if (msg.IsQuery && msg.Questions.Count > 0)
            {
                if (QueryReceived is not null)
                    await QueryReceived(new MessageEventArgs { Message = msg, RemoteEndPoint = result.RemoteEndPoint });
            }
            else if (msg is { IsResponse: true, Answers.Count: > 0 } && AnswerReceived is not null)
            {
                await AnswerReceived(new MessageEventArgs { Message = msg, RemoteEndPoint = result.RemoteEndPoint });
            }
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Receive handler failed");
            // eat the exception
        }
    }

    #region IDisposable Support

    /// <summary>
    ///  Dispose of the resources.
    /// </summary>
    /// <param name="disposing"></param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            //Dispose of the clients
            _unicastClientIp4?.Dispose();
            _unicastClientIp6?.Dispose();
            Stop();
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