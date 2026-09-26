using System.Buffers;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Channels;
using System.Threading.RateLimiting;
using System.Xml.Linq;

using Microsoft.Extensions.Logging;

namespace Oppo;

public sealed class MagnetarClient(string hostName, string macAddress, ILogger<MagnetarClient> logger) : IOppoClient
{
    private const int Port = 8102;

    // The reference app has no such cap and just keeps appending until a self-contained chunk
    // resets the buffer; this guards against unbounded growth if the player ever sends something
    // that never closes.
    private const int MaxPushBufferSize = 65536;

    private static ReadOnlySpan<byte> MessageOpenBytes => "<message>"u8;
    private static ReadOnlySpan<byte> MessageCloseBytes => "</message>"u8;

    private readonly ILogger<MagnetarClient> _logger = logger;
    private readonly string _hostName = hostName;
    private readonly IPAddress _ipAddress = IPAddress.Parse(hostName);
    private readonly string _macAddress = macAddress;

    private readonly TcpClient _tcpClient = ConnectHelper.CreateTcpClient();
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly TimeSpan _timeout = TimeSpan.FromSeconds(3);
    private readonly TokenBucketRateLimiter _rateLimiter = ConnectHelper.CreateRateLimiter();

    private readonly Lock _streamingSync = new();
    private Channel<OppoStreamingEvent>? _streamingChannel;
    private CancellationTokenSource? _readerCts;
    private Task? _readerTask;
    private bool _identifySent;

    public string HostName => _hostName;

    internal bool IsDisposed { get; private set; }

    private PowerState _lastPowerState = PowerState.Off;

    public async ValueTask<OppoResult<PowerState>> PowerToggleAsync(CancellationToken cancellationToken = default)
    {
        CancellationTokenSource? cancellationTokenSource;
        if (_lastPowerState == PowerState.Off)
        {
            await WakeOnLan.SendWakeOnLanAsync(_macAddress, _ipAddress, _logger);
            cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cancellationTokenSource.CancelAfter(_timeout);
        }
        else
            cancellationTokenSource = null;

        using (cancellationTokenSource)
        {
            try
            {
                var result = await SendCommand("#POW", cancellationToken, cancellationTokenSource?.Token);
                if (!result.Success)
                    return false;

                _lastPowerState = _lastPowerState switch
                {
                    PowerState.Off => PowerState.On,
                    PowerState.On => PowerState.Off,
                    _ => _lastPowerState
                };
                return new OppoResult<PowerState> { Success = true, Result = _lastPowerState };
            }
            catch (Exception e)
            {
                if (e is OperationCanceledException && cancellationTokenSource is { IsCancellationRequested: true })
                {
                    _lastPowerState = _lastPowerState switch
                    {
                        PowerState.Off => PowerState.On,
                        PowerState.On => PowerState.Off,
                        _ => _lastPowerState
                    };

                    return new OppoResult<PowerState> { Success = true, Result = _lastPowerState };
                }

                _logger.FailedToSendCommandException(e);
                return false;
            }
        }
    }

    public async ValueTask<OppoResult<PowerState>> PowerOnAsync(CancellationToken cancellationToken = default)
    {
        await WakeOnLan.SendWakeOnLanAsync(_macAddress, _ipAddress, _logger);
        using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cancellationTokenSource.CancelAfter(_timeout);
        try
        {
            var result = await SendCommand("#PON", cancellationToken, cancellationTokenSource.Token);
            if (!result.Success)
                return false;

            _lastPowerState = PowerState.On;
            return new OppoResult<PowerState> { Success = true, Result = PowerState.On };
        }
        catch (Exception e)
        {
            if (e is OperationCanceledException && cancellationTokenSource.IsCancellationRequested)
            {
                _lastPowerState = PowerState.On;
                return new OppoResult<PowerState> { Success = true, Result = PowerState.On };
            }

            _logger.FailedToSendCommandException(e);
            return false;
        }
    }

    public async ValueTask<OppoResult<PowerState>> PowerOffAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommand("#POF", cancellationToken);
        if (!result.Success)
            return false;

        _lastPowerState = PowerState.Off;
        return new OppoResult<PowerState>
        {
            Success = true,
            Result = PowerState.Off
        };
    }

    public async ValueTask<OppoResult<TrayState>> EjectToggleAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommand("#EJT", cancellationToken);
        return result.Success;
    }

    public async ValueTask<bool> PlayAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommand("#PLA", cancellationToken);
        return result.Success;
    }

    public async ValueTask<bool> StopAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommand("#STP", cancellationToken);
        return result.Success;
    }

    public async ValueTask<bool> PauseAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommand("#PAU", cancellationToken);
        return result.Success;
    }

    public async ValueTask<bool> NextAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommand("#NXT", cancellationToken);
        return result.Success;
    }

    public async ValueTask<bool> PreviousAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommand("#PRE", cancellationToken);
        return result.Success;
    }

    public async ValueTask<bool> UpArrowAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommand("#NUP", cancellationToken);
        return result.Success;
    }

    public async ValueTask<bool> DownArrowAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommand("#NDN", cancellationToken);
        return result.Success;
    }

    public async ValueTask<bool> LeftArrowAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommand("#NLT", cancellationToken);
        return result.Success;
    }

    public async ValueTask<bool> RightArrowAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommand("#NRT", cancellationToken);
        return result.Success;
    }

    public async ValueTask<bool> EnterAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommand("#SEL", cancellationToken);
        return result.Success;
    }

    public async ValueTask<bool> HomeAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommand("#HOM", cancellationToken);
        return result.Success;
    }

    public async ValueTask<bool> SetupAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommand("#SET", cancellationToken);
        return result.Success;
    }

    public async ValueTask<bool> ReturnAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommand("#RET", cancellationToken);
        return result.Success;
    }

    public async ValueTask<bool> NumericInputAsync(ushort number, CancellationToken cancellationToken = default)
    {
        if (number > 9) return false;
        var result = await SendCommand($"#NU{number}", cancellationToken);
        return result.Success;
    }

    public async ValueTask<OppoResult<DimmerState>> DimmerAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommand("#DIM", cancellationToken);
        return result.Success;
    }

    public async ValueTask<OppoResult<PureAudioState>> PureAudioToggleAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommand("#PUR", cancellationToken);
        return result.Success;
    }

    public async ValueTask<OppoResult<ushort?>> VolumeUpAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommand("#VUP", cancellationToken);
        return result.Success;
    }

    public async ValueTask<OppoResult<ushort?>> VolumeDownAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommand("#VDN", cancellationToken);
        return result.Success;
    }

    public async ValueTask<OppoResult<MuteState>> MuteToggleAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommand("#MUT", cancellationToken);
        return result.Success;
    }

    public async ValueTask<bool> ClearAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommand("#CLR", cancellationToken);
        return result.Success;
    }

    public async ValueTask<bool> GoToAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommand("#GOT", cancellationToken);
        return result.Success;
    }

    public ValueTask<bool> PageUpAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult(false);
    public ValueTask<bool> PageDownAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult(false);

    public async ValueTask<bool> InfoToggleAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommand("#OSD", cancellationToken);
        return result.Success;
    }

    public async ValueTask<bool> TopMenuAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommand("#TTL", cancellationToken);
        return result.Success;
    }

    public async ValueTask<bool> PopUpMenuAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommand("#MNU", cancellationToken);
        return result.Success;
    }

    public async ValueTask<bool> RedAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommand("#RED", cancellationToken);
        return result.Success;
    }

    public async ValueTask<bool> GreenAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommand("#GRN", cancellationToken);
        return result.Success;
    }

    public async ValueTask<bool> BlueAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommand("#BLU", cancellationToken);
        return result.Success;
    }

    public async ValueTask<bool> YellowAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommand("#YLW", cancellationToken);
        return result.Success;
    }

    public async ValueTask<OppoResult<ushort?>> ReverseAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommand("#REV", cancellationToken);
        return result.Success;
    }

    public async ValueTask<OppoResult<ushort?>> ForwardAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommand("#FWD", cancellationToken);
        return result.Success;
    }

    public async ValueTask<bool> AudioAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommand("#AUD", cancellationToken);
        return result.Success;
    }

    public async ValueTask<bool> SubtitleAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommand("#SUB", cancellationToken);
        return result.Success;
    }

    public async ValueTask<OppoResult<string>> AngleAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommand("#ANG", cancellationToken);
        return result.Success;
    }

    public async ValueTask<OppoResult<string>> ZoomAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommand("#ZOM", cancellationToken);
        return result.Success;
    }

    public async ValueTask<OppoResult<string>> SecondaryAudioProgramAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommand("#SAP", cancellationToken);
        return result.Success;
    }

    public async ValueTask<OppoResult<ABReplayState>> ABReplayAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommand("#ATB", cancellationToken);
        return result.Success;
    }

    public async ValueTask<OppoResult<RepeatState>> RepeatAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommand("#RPT", cancellationToken);
        return result.Success;
    }

    public async ValueTask<OppoResult<string>> PictureInPictureAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommand("#PIP", cancellationToken);
        return result.Success;
    }

    public async ValueTask<bool> ResolutionAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommand("#HDM", cancellationToken);
        return result.Success;
    }

    public async ValueTask<bool> SubtitleHoldAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommand("#SUH", cancellationToken);
        return result.Success;
    }

    public async ValueTask<bool> OptionAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommand("#OPT", cancellationToken);
        return result.Success;
    }

    public ValueTask<bool> ThreeDAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult(false);
    public ValueTask<bool> PictureAdjustmentAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult(false);

    public async ValueTask<bool> HDRAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommand("#HDR", cancellationToken);
        return result.Success;
    }

    public ValueTask<bool> InfoHoldAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult(false);
    public ValueTask<bool> ResolutionHoldAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult(false);
    public ValueTask<bool> AVSyncAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult(false);
    public ValueTask<bool> GaplessPlayAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult(false);
    public ValueTask<bool> NoopAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult(false);
    public ValueTask<bool> InputAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult(false);

    public ValueTask<OppoResult<RepeatMode>> SetRepeatAsync(RepeatMode mode, CancellationToken cancellationToken = default)
        => ValueTask.FromResult(new OppoResult<RepeatMode> { Success = false });
    public ValueTask<OppoResult<ushort>> SetVolumeAsync(ushort volume, CancellationToken cancellationToken = default)
        => ValueTask.FromResult(new OppoResult<ushort> { Success = false });
    public ValueTask<OppoResult<VolumeInfo>> QueryVolumeAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult(new OppoResult<VolumeInfo> { Success = false });
    public ValueTask<OppoResult<PowerState>> QueryPowerStatusAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult(new OppoResult<PowerState> { Success = false });
    public ValueTask<OppoResult<PlaybackStatus>> QueryPlaybackStatusAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult(new OppoResult<PlaybackStatus> { Success = false });
    public ValueTask<OppoResult<HDMIResolution>> QueryHDMIResolutionAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult(new OppoResult<HDMIResolution> { Success = false });
    public ValueTask<OppoResult<uint>> QueryTrackOrTitleElapsedTimeAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult(new OppoResult<uint> { Success = false });
    public ValueTask<OppoResult<uint>> QueryTrackOrTitleRemainingTimeAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult(new OppoResult<uint> { Success = false });
    public ValueTask<OppoResult<uint>> QueryChapterElapsedTimeAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult(new OppoResult<uint> { Success = false });
    public ValueTask<OppoResult<uint>> QueryChapterRemainingTimeAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult(new OppoResult<uint> { Success = false });
    public ValueTask<OppoResult<uint>> QueryTotalElapsedTimeAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult(new OppoResult<uint> { Success = false });
    public ValueTask<OppoResult<uint>> QueryTotalRemainingTimeAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult(new OppoResult<uint> { Success = false });
    public ValueTask<OppoResult<DiscType>> QueryDiscTypeAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult(new OppoResult<DiscType> { Success = false });
    public ValueTask<OppoResult<string>> QueryAudioTypeAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult(new OppoResult<string> { Success = false });
    public ValueTask<OppoResult<string>> QuerySubtitleTypeAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult(new OppoResult<string> { Success = false });
    public ValueTask<OppoResult<bool>> QueryThreeDStatusAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult(new OppoResult<bool> { Success = false });
    public ValueTask<OppoResult<HDRStatus>> QueryHDRStatusAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult(new OppoResult<HDRStatus> { Success = false });
    public ValueTask<OppoResult<AspectRatio>> QueryAspectRatioAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult(new OppoResult<AspectRatio> { Success = false });
    public ValueTask<OppoResult<CurrentRepeatMode>> QueryRepeatModeAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult(new OppoResult<CurrentRepeatMode> { Success = false });
    public ValueTask<OppoResult<InputSource>> QueryInputSourceAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult(new OppoResult<InputSource> { Success = false });
    public ValueTask<OppoResult<InputSource>> SetInputSourceAsync(InputSource inputSource, CancellationToken cancellationToken = default)
        => ValueTask.FromResult(new OppoResult<InputSource> { Success = false });
    public ValueTask<OppoResult<string>> QueryCDDBNumberAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult(new OppoResult<string> { Success = false });
    public ValueTask<OppoResult<string>> QueryTrackNameAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult(new OppoResult<string> { Success = false });
    public ValueTask<OppoResult<string>> QueryTrackAlbumAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult(new OppoResult<string> { Success = false });
    public ValueTask<OppoResult<string>> QueryTrackPerformerAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult(new OppoResult<string> { Success = false });
    public ValueTask<OppoResult<VerboseMode>> QueryVerboseMode(CancellationToken cancellationToken = default)
        => ValueTask.FromResult(new OppoResult<VerboseMode> { Success = false });
    public ValueTask<OppoResult<VerboseMode>> SetVerboseMode(VerboseMode verboseMode, CancellationToken cancellationToken = default)
        => ValueTask.FromResult(new OppoResult<VerboseMode> { Success = false });

    public bool SupportsStreamingUpdates => true;

    public async IAsyncEnumerable<OppoStreamingEvent> SubscribeStreamingUpdates([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        Channel<OppoStreamingEvent> channel;
        lock (_streamingSync)
        {
            if (_streamingChannel is not null)
            {
                _logger.ReplacingStreamingSubscriber();
                _streamingChannel.Writer.TryComplete();
            }

            channel = Channel.CreateBounded<OppoStreamingEvent>(new BoundedChannelOptions(128)
            {
                SingleReader = true,
                SingleWriter = true,
                FullMode = BoundedChannelFullMode.DropOldest,
                AllowSynchronousContinuations = false
            });
            _streamingChannel = channel;
        }

        try
        {
            await EnsureStreamingStartedAsync(cancellationToken);

            await foreach (var evt in channel.Reader.ReadAllAsync(cancellationToken))
                yield return evt;
        }
        finally
        {
            lock (_streamingSync)
            {
                if (ReferenceEquals(_streamingChannel, channel))
                {
                    _streamingChannel = null;
                    channel.Writer.TryComplete();
                }
            }
        }
    }

    public ValueTask<bool> IsConnectedAsync(TimeSpan? timeout = null)
        => ConnectHelper.IsConnectedAsync(_tcpClient, _hostName, Port, _semaphore, _logger, timeout);

    /// <summary>
    /// Identifies as an app (undocumented <c>#APP</c> command) so the player starts pushing
    /// unsolicited <c>&lt;message&gt;</c> state updates on this connection, then starts the
    /// background reader loop that parses them, if not already running.
    /// </summary>
    private async ValueTask EnsureStreamingStartedAsync(CancellationToken cancellationToken)
    {
        if (!_identifySent)
        {
            var identifyResult = await SendCommand("#APP", cancellationToken);
            _identifySent = identifyResult.Success;
        }

        if (_readerTask is { IsCompleted: false })
            return;

        lock (_streamingSync)
        {
            if (_readerTask is { IsCompleted: false })
                return;

            if (!_tcpClient.Connected)
                return;

            var pipeReader = PipeReader.Create(_tcpClient.GetStream());
            var readerCts = new CancellationTokenSource();
            _readerCts = readerCts;
            _readerTask = Task.Run(() => ReaderLoopAsync(pipeReader, readerCts.Token), readerCts.Token);
        }
    }

    private async Task ReaderLoopAsync(PipeReader pipeReader, CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                ReadResult readResult;
                try
                {
                    readResult = await pipeReader.ReadAsync(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception e)
                {
                    _logger.MagnetarStreamingConnectionLost(e);
                    break;
                }

                var buffer = readResult.Buffer;

                while (TryExtractMessage(ref buffer, out var messageXml))
                {
                    var evt = TryParsePushMessage(messageXml);
                    if (evt is not null)
                        PublishStreamingEvent(evt);
                }

                // No complete message found and the unconsumed remainder (e.g. an opening tag with no
                // close tag yet, or pure junk that never matched) has grown past the cap - the reference
                // app has no such cap and just keeps appending forever, so this guards against unbounded
                // growth if the player ever sends something that never closes.
                if (buffer.Length > MaxPushBufferSize)
                {
                    _logger.MagnetarPushBufferExceeded(MaxPushBufferSize);
                    buffer = buffer.Slice(buffer.End);
                }

                pipeReader.AdvanceTo(buffer.Start, buffer.End);

                if (readResult.IsCompleted)
                    break;
            }
        }
        finally
        {
            _identifySent = false;

            Channel<OppoStreamingEvent>? streamingChannel;
            lock (_streamingSync)
            {
                streamingChannel = _streamingChannel;
                _streamingChannel = null;
            }

            streamingChannel?.Writer.TryComplete();

            await pipeReader.CompleteAsync();
        }
    }

    /// <summary>
    /// Pulls one complete <c>&lt;message&gt;...&lt;/message&gt;</c> span out of <paramref name="buffer"/>,
    /// operating directly on the pipe's <see cref="ReadOnlySequence{T}"/> - no string/byte[] copies until
    /// a full message is found. Any bytes preceding the opening tag (e.g. a stray "ack" reply to a
    /// fire-and-forget command sent on the same connection) are silently dropped once a message completes.
    /// </summary>
    private static bool TryExtractMessage(ref ReadOnlySequence<byte> buffer, out ReadOnlySequence<byte> messageXml)
    {
        messageXml = default;

        var reader = new SequenceReader<byte>(buffer);
        if (!reader.TryReadTo(out ReadOnlySequence<byte> _, MessageOpenBytes, advancePastDelimiter: false))
            return false;

        var messageStart = reader.Position;
        reader.Advance(MessageOpenBytes.Length);

        if (!reader.TryReadTo(out ReadOnlySequence<byte> _, MessageCloseBytes, advancePastDelimiter: true))
            return false;

        messageXml = buffer.Slice(messageStart, reader.Position);
        buffer = buffer.Slice(reader.Position);
        return true;
    }

    private void PublishStreamingEvent(OppoStreamingEvent evt)
    {
        Channel<OppoStreamingEvent>? channel;
        lock (_streamingSync)
            channel = _streamingChannel;

        channel?.Writer.TryWrite(evt);
    }

    private OppoStreamingEvent? TryParsePushMessage(in ReadOnlySequence<byte> xml)
    {
        var length = checked((int)xml.Length);
        var rented = ArrayPool<byte>.Shared.Rent(length);
        try
        {
            XElement root;
            try
            {
                xml.CopyTo(rented);
                using var stream = new MemoryStream(rented, 0, length, writable: false);
                root = XElement.Load(stream);
            }
            catch (Exception e)
            {
                _logger.FailedToParseMagnetarPushMessage(Encoding.UTF8.GetString(rented, 0, length), e);
                return null;
            }

            var operation = root.Element("operation");
            var data = operation?.Element("data");
            if (data is null)
                return null;

            return (string?)operation!.Element("cmd") switch
            {
                "UpdatePlayState" => ParsePlayState(data),
                "UpdateVolume" => ParseVolumeUpdate(data),
                _ => null
            };
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rented);
        }
    }

    private static OppoMagnetarPlayStateStreamingEvent? ParsePlayState(XElement data)
    {
        var mediaType = data.Element("media")?.Attribute("type")?.Value;
        if (string.IsNullOrEmpty(mediaType))
            return null;

        return new OppoMagnetarPlayStateStreamingEvent(
            MediaType: mediaType,
            State: GetText(data, "state") ?? "",
            CurrTime: GetText(data, "curr_time") ?? "",
            TotalTime: GetText(data, "total_time") ?? "",
            RepeatMode: GetText(data, "repeat_mode") ?? "",
            TrackTitle: GetText(data, "track_title"),
            DiscArtist: GetText(data, "disc_artist"),
            DiscTitle: GetText(data, "disc_title"),
            FileName: GetText(data, "file_name"),
            Artist: GetText(data, "artist"),
            Title: GetText(data, "title"),
            Hdr: GetText(data, "hdr"),
            FourK: GetText(data, "four_k"),
            FrameRate: GetText(data, "frame_rate"));
    }

    private static OppoVolumeStreamingEvent ParseVolumeUpdate(XElement data)
    {
        var muted = (GetText(data, "mute") ?? "").AsSpan().Trim().Equals("true", StringComparison.OrdinalIgnoreCase);
        var volume = ushort.TryParse(GetText(data, "volume"), out var parsedVolume) ? parsedVolume : (ushort?)null;
        return new OppoVolumeStreamingEvent(new VolumeInfo(volume, muted));
    }

    /// <summary>
    /// Returns a child element's text, or null if missing/empty.
    ///
    /// The player's own XML serializer double-escapes entities (confirmed against a real capture:
    /// literal <c>&amp;amp;amp;</c> on the wire, which decodes to <c>&amp;amp;</c> after normal XML
    /// parsing instead of <c>&amp;</c>). A second HTML-decode pass resolves that; it's a no-op for
    /// text that was only escaped once.
    /// </summary>
    private static string? GetText(XElement data, string tag)
    {
        var value = data.Element(tag)?.Value;
        if (string.IsNullOrEmpty(value))
            return null;

        // Skip the decode pass entirely when there's nothing to unescape - the common case.
        return value.Contains('&', StringComparison.Ordinal) ? WebUtility.HtmlDecode(value) : value;
    }

    private static readonly byte[] CarriageReturnLineFeed = "\r\n"u8.ToArray();

    private async ValueTask<OppoResultCore> SendCommand(string command, CancellationToken cancellationToken, CancellationToken? commandCancellationToken = null, [CallerMemberName] string? caller = null)
    {
        using var lease = await _rateLimiter.AcquireAsyncWithoutCancellationException(_logger, cancellationToken, caller);
        if (!lease.IsAcquired)
        {
            _logger.FailedToAcquireRateLimitLease(caller);
            return OppoResultCore.FalseResult;
        }

        if (!await _semaphore.WaitAsyncWithoutCancellationException(_logger, _timeout, cancellationToken, caller))
            return OppoResultCore.FalseResult;

        try
        {
            if (_logger.IsEnabled(LogLevel.Trace))
                _logger.SendingCommand(command);

            // Connect without semaphore - we already acquired lock
            await ConnectHelper.IsConnectedNoLockAsync(_tcpClient, _hostName, Port, _logger);

            var networkStream = _tcpClient.GetStream();
            await networkStream.WriteAsync(Encoding.ASCII.GetBytes(command), commandCancellationToken ?? cancellationToken);
            await networkStream.WriteAsync(CarriageReturnLineFeed, commandCancellationToken ?? cancellationToken);

            return OppoResultCore.SuccessResult("ack");
        }
        catch (Exception e)
        {
            // let the caller take care of the exception since it specified a different token for the command
            if (e is OperationCanceledException && commandCancellationToken is { IsCancellationRequested: true })
                throw;

            _logger.FailedToSendCommandException(e);
            return OppoResultCore.FalseResult;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public void Dispose()
    {
        _readerCts?.Cancel();
        _readerCts?.Dispose();
        _rateLimiter.Dispose();
        _tcpClient.Dispose();
        _semaphore.Dispose();
        IsDisposed = true;
    }
}
