using System.Buffers;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;

using Microsoft.Extensions.Logging;

namespace Oppo;

public sealed class MagnetarClient(string hostName, ILogger<MagnetarClient> logger) : IOppoClient
{
    private const int Port = 8102;
    private readonly ILogger<MagnetarClient> _logger = logger;
    private readonly string _hostName = hostName;

    private TcpClient _tcpClient = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly TimeSpan _timeout = TimeSpan.FromSeconds(1);
    private readonly StringBuilder _stringBuilder = new();

    public string HostName => _hostName;

    // Power Toggle
    public async ValueTask<OppoResult<PowerState>> PowerToggleAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommandWithRetry("#POW", "#POW ?", cancellationToken);
        return result.Success switch
        {
            false => false,
            _ => new OppoResult<PowerState>
            {
                Success = true,
                Result = result.Response switch
                {
                    var response when response.Contains("ON", StringComparison.OrdinalIgnoreCase) => PowerState.On,
                    var response when response.Contains("OFF", StringComparison.OrdinalIgnoreCase) => PowerState.Off,
                    _ => LogError(result.Response, PowerState.Unknown)
                }
            }
        };
    }

    public async ValueTask<OppoResult<PowerState>> PowerOnAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommandWithRetry("#PON", "#POW ?", cancellationToken);
        return result.Success switch
        {
            false => false,
            _ => new OppoResult<PowerState>
            {
                Success = true,
                Result = result.Response switch
                {
                    var response when response.Contains("ON", StringComparison.OrdinalIgnoreCase) => PowerState.On,
                    var response when response.Contains("OFF", StringComparison.OrdinalIgnoreCase) => PowerState.Off,
                    _ => LogError(result.Response, PowerState.Unknown)
                }
            }
        };
    }

    public async ValueTask<OppoResult<PowerState>> PowerOffAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommandWithRetry("#POF", "#POW ?", cancellationToken);
        return result.Success switch
        {
            false => false,
            _ => new OppoResult<PowerState>
            {
                Success = true,
                Result = result.Response switch
                {
                    var response when response.Contains("ON", StringComparison.OrdinalIgnoreCase) => PowerState.On,
                    var response when response.Contains("OFF", StringComparison.OrdinalIgnoreCase) => PowerState.Off,
                    _ => LogError(result.Response, PowerState.Unknown)
                }
            }
        };
    }

    public async ValueTask<OppoResult<TrayState>> EjectToggleAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommandWithRetry("#EJT", "#EJT ?", cancellationToken);
        return result.Success;
    }

    public async ValueTask<bool> PlayAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommandWithRetry("#PLA", null, cancellationToken);
        return result.Success;
    }

    public async ValueTask<bool> StopAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommandWithRetry("#STP", null, cancellationToken);
        return result.Success;
    }

    public async ValueTask<bool> PauseAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommandWithRetry("#PAU", null, cancellationToken);
        return result.Success;
    }

    public async ValueTask<bool> NextAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommandWithRetry("#NXT", null, cancellationToken);
        return result.Success;
    }

    public async ValueTask<bool> PreviousAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommandWithRetry("#PRE", null, cancellationToken);
        return result.Success;
    }

    public async ValueTask<bool> UpArrowAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommandWithRetry("#NUP", null, cancellationToken);
        return result.Success;
    }

    public async ValueTask<bool> DownArrowAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommandWithRetry("#NDN", null, cancellationToken);
        return result.Success;
    }

    public async ValueTask<bool> LeftArrowAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommandWithRetry("#NLT", null, cancellationToken);
        return result.Success;
    }

    public async ValueTask<bool> RightArrowAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommandWithRetry("#NRT", null, cancellationToken);
        return result.Success;
    }

    public async ValueTask<bool> EnterAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommandWithRetry("#SEL", null, cancellationToken);
        return result.Success;
    }

    public async ValueTask<bool> HomeAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommandWithRetry("#HOM", null, cancellationToken);
        return result.Success;
    }

    public async ValueTask<bool> SetupAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommandWithRetry("#SET", "#SET ?", cancellationToken);
        return result.Success;
    }

    public async ValueTask<bool> ReturnAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommandWithRetry("#RET", "#RET ?", cancellationToken);
        return result.Success;
    }

    public async ValueTask<bool> NumericInputAsync(ushort number, CancellationToken cancellationToken = default)
    {
        if (number > 9) return false;
        var result = await SendCommandWithRetry($"#NU{number}", null, cancellationToken);
        return result.Success;
    }

    public async ValueTask<OppoResult<DimmerState>> DimmerAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommandWithRetry("#DIM", "#DIM ?", cancellationToken);
        return result.Success;
    }

    public async ValueTask<OppoResult<PureAudioState>> PureAudioToggleAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommandWithRetry("#PUR", "#PUR ?", cancellationToken);
        return result.Success;
    }

    public async ValueTask<OppoResult<ushort?>> VolumeUpAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommandWithRetry("#VUP", "#VUP ?", cancellationToken);
        return result.Success;
    }

    public async ValueTask<OppoResult<ushort?>> VolumeDownAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommandWithRetry("#VDN", "#VDN ?", cancellationToken);
        return result.Success;
    }

    public async ValueTask<OppoResult<MuteState>> MuteToggleAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommandWithRetry("#MUT", "#MUT ?", cancellationToken);
        return result.Success;
    }

    public async ValueTask<bool> ClearAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommandWithRetry("#CLR", null, cancellationToken);
        return result.Success;
    }

    public async ValueTask<bool> GoToAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommandWithRetry("#GOT", null, cancellationToken);
        return result.Success;
    }

    public ValueTask<bool> PageUpAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult(false);
    public ValueTask<bool> PageDownAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult(false);

    public async ValueTask<bool> InfoToggleAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommandWithRetry("#OSD", null, cancellationToken);
        return result.Success;
    }

    public async ValueTask<bool> TopMenuAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommandWithRetry("#TTL", null, cancellationToken);
        return result.Success;
    }

    public async ValueTask<bool> PopUpMenuAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommandWithRetry("#MNU", null, cancellationToken);
        return result.Success;
    }

    public async ValueTask<bool> RedAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommandWithRetry("#RED", null, cancellationToken);
        return result.Success;
    }

    public async ValueTask<bool> GreenAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommandWithRetry("#GRN", null, cancellationToken);
        return result.Success;
    }

    public async ValueTask<bool> BlueAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommandWithRetry("#BLU", null, cancellationToken);
        return result.Success;
    }

    public async ValueTask<bool> YellowAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommandWithRetry("#YLW", null, cancellationToken);
        return result.Success;
    }

    public async ValueTask<OppoResult<ushort?>> ReverseAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommandWithRetry("#REV", null, cancellationToken);
        return result.Success;
    }

    public async ValueTask<OppoResult<ushort?>> ForwardAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommandWithRetry("#FWD", null, cancellationToken);
        return result.Success;
    }

    public async ValueTask<bool> AudioAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommandWithRetry("#AUD", null, cancellationToken);
        return result.Success;
    }

    public async ValueTask<bool> SubtitleAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommandWithRetry("#SUB", null, cancellationToken);
        return result.Success;
    }

    public async ValueTask<OppoResult<string>> AngleAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommandWithRetry("#ANG", "#ANG ?", cancellationToken);
        return result.Success;
    }

    public async ValueTask<OppoResult<string>> ZoomAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommandWithRetry("#ZOM", "#ZOM ?", cancellationToken);
        return result.Success;
    }

    public async ValueTask<OppoResult<string>> SecondaryAudioProgramAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommandWithRetry("#SAP", null, cancellationToken);
        return result.Success;
    }

    public async ValueTask<OppoResult<ABReplayState>> ABReplayAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommandWithRetry("#ATB", "#ATB ?", cancellationToken);
        return result.Success;
    }

    public async ValueTask<OppoResult<RepeatState>> RepeatAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommandWithRetry("#RPT", "#RPT ?", cancellationToken);
        return result.Success;
    }

    public async ValueTask<OppoResult<string>> PictureInPictureAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommandWithRetry("#PIP", "#PIP ?", cancellationToken);
        return result.Success;
    }

    public async ValueTask<bool> ResolutionAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommandWithRetry("#HDM", null, cancellationToken);
        return result.Success;
    }

    public async ValueTask<bool> SubtitleHoldAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommandWithRetry("#SUH", null, cancellationToken);
        return result.Success;
    }

    public async ValueTask<bool> OptionAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommandWithRetry("#OPT", null, cancellationToken);
        return result.Success;
    }

    public ValueTask<bool> ThreeDAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult(false);
    public ValueTask<bool> PictureAdjustmentAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult(false);

    public async ValueTask<bool> HDRAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommandWithRetry("#HDR", null, cancellationToken);
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
    public ValueTask<OppoResult<VerboseMode>> SetVerboseMode(VerboseMode verboseMode, CancellationToken cancellationToken = default)
        => ValueTask.FromResult(new OppoResult<VerboseMode> { Success = false });

    public async ValueTask<bool> IsConnectedAsync(TimeSpan? timeout = null)
    {
        if (_tcpClient.Connected)
            return _tcpClient.Connected;

        var acquired = await _semaphore.WaitAsync(timeout ?? TimeSpan.FromSeconds(5));
        if (!acquired)
            return _tcpClient.Connected;

        try
        {
            if (_tcpClient.Connected)
                return _tcpClient.Connected;

            return await DoConnect(true);
        }
        finally
        {
            _semaphore.Release();
        }

        async ValueTask<bool> DoConnect(bool allowRetry)
        {
            try
            {
                using var cancellationTokenSource = new CancellationTokenSource(timeout ?? TimeSpan.FromSeconds(3));
                await _tcpClient.ConnectAsync(_hostName, Port, cancellationTokenSource.Token);
                return _tcpClient.Connected;
            }
            catch (OperationCanceledException)
            {
                // nothing to do here, ignore
            }
            catch (SocketException) when (allowRetry)
            {
                // Network stack might not be ready, wait a bit and try one more time
                _logger.RetryingConnectionAfterSocketException(_hostName, Port);
                await Task.Delay(500);
                return await DoConnect(false);
            }
            catch (Exception e)
            {
                _logger.FailedToConnectToOppoPlayer(e, _hostName, Port);
            }

            return _tcpClient.Connected;
        }
    }

    private static readonly byte[] CarriageReturnLineFeed = "\r\n"u8.ToArray();
    private async ValueTask<OppoResultCore> SendCommand(string command, string? query, CancellationToken cancellationToken,
        [CallerMemberName] string? caller = null)
    {
        if (!await _semaphore.WaitAsync(_timeout, cancellationToken))
            return OppoResultCore.FalseResult;

        try
        {
            if (_logger.IsEnabled(LogLevel.Trace))
                _logger.SendingCommand(command);

            if (Interlocked.CompareExchange(ref _failedResponseCount, 0, 2) > 2)
            {
                _logger.TooManyFailedResponses(caller);

                _tcpClient.Close();
                _tcpClient = new TcpClient();
                await IsConnectedAsync();
            }

            var networkStream = _tcpClient.GetStream();
            await networkStream.WriteAsync(Encoding.ASCII.GetBytes(command), cancellationToken);
            await networkStream.WriteAsync(CarriageReturnLineFeed, cancellationToken);

            if (!_tcpClient.Connected)
                await _tcpClient.ConnectAsync(_hostName, Port, cancellationToken);

            var response = await ReadUntilCarriageReturnAsync(networkStream, cancellationToken);
            if (!string.Equals(response, "ack", StringComparison.OrdinalIgnoreCase))
            {
                _logger.FailedToSendCommand(caller, response);
                return OppoResultCore.FalseResult;
            }

            // Query status
            if (string.IsNullOrEmpty(query))
                return OppoResultCore.SuccessResult(response);

            await networkStream.WriteAsync(Encoding.ASCII.GetBytes(query), cancellationToken);
            await networkStream.WriteAsync(CarriageReturnLineFeed, cancellationToken);
            response = await ReadUntilCarriageReturnAsync(networkStream, cancellationToken);
            _logger.ReceivedResponse(response);
            return OppoResultCore.SuccessResult(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Magnetar command failed");
            return OppoResultCore.FalseResult;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async ValueTask<OppoResultCore> SendCommandWithRetry(string command, string? query, CancellationToken cancellationToken, [CallerMemberName] string? caller = null)
    {
        var result = await SendCommand(command, query, cancellationToken, caller);
        if (!result.InvalidVerboseLevel)
            return result;

        var verboseMode = await SetVerboseMode(VerboseMode.Off, cancellationToken);
        return !verboseMode.Success ? OppoResultCore.FalseResult : await SendCommand(command, query, cancellationToken, caller);
    }

    private async ValueTask<string> ReadUntilCarriageReturnAsync(NetworkStream networkStream, CancellationToken cancellationToken)
    {
        _stringBuilder.Clear();
        var pipeReader = PipeReader.Create(networkStream);
        var charBuffer = ArrayPool<char>.Shared.Rent(1024);
        var firstWrite = true;
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(3));

        try
        {
            while (true)
            {
                var result = await pipeReader.ReadAsync(cancellationTokenSource.Token);
                var buffer = result.Buffer;

                do
                {
                    var position = buffer.PositionOf((byte)'\n');

                    if (position != null)
                    {
                        var slice = buffer.Slice(0, position.Value);
                        if (slice.IsSingleSegment)
                        {
                            WriteSpan(slice.FirstSpan, charBuffer, ref firstWrite);
                        }
                        else
                        {
                            foreach (var segment in slice)
                            {
                                WriteSpan(segment.Span, charBuffer, ref firstWrite);
                            }
                        }

                        pipeReader.AdvanceTo(slice.End);
                        return _stringBuilder.ToString();
                    }

                    foreach (var segment in buffer)
                    {
                        WriteSpan(segment.Span, charBuffer, ref firstWrite);
                    }
                    pipeReader.AdvanceTo(buffer.End);
                } while (!result.IsCompleted && !cancellationToken.IsCancellationRequested);

                if (result.IsCompleted || cancellationToken.IsCancellationRequested)
                    break;
            }
        }
        finally
        {
            ArrayPool<char>.Shared.Return(charBuffer);
        }

        return _stringBuilder.ToString();
    }

    private void WriteSpan(in ReadOnlySpan<byte> span, char[] charBuffer, ref bool isFirstWrite)
    {
        if (isFirstWrite)
        {
            // Models prior to UDP-20X doesn't send back @OK or @ER, rather it's @(COMMAND_CODE) followed by OK|ER and then the response
            if (!span.StartsWith("@OK "u8) && !span.StartsWith("@ER "u8))
            {
                _stringBuilder.Append('@');
                int charsDecoded = Encoding.ASCII.GetChars(span[5..], charBuffer);
                _stringBuilder.Append(charBuffer, 0, charsDecoded);
            }
            else
            {
                int charsDecoded = Encoding.ASCII.GetChars(span, charBuffer);
                _stringBuilder.Append(charBuffer, 0, charsDecoded);
            }

            isFirstWrite = false;
        }
        else
        {
            int charsDecoded = Encoding.ASCII.GetChars(span, charBuffer);
            _stringBuilder.Append(charBuffer, 0, charsDecoded);
        }
    }

    private uint _failedResponseCount;

    private TEnum LogError<TEnum>(string response, TEnum returnValue, [CallerMemberName]string? callerMemberName = null)
        where TEnum : Enum
    {
        _logger.CallerMemberFailed(callerMemberName, response);
        return returnValue;
    }

    public void Dispose()
    {
        _tcpClient.Dispose();
        _semaphore.Dispose();
    }
}