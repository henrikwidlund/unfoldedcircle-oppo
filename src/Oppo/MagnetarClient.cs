using System.Net;
using System.Net.Sockets;
using System.Text;

using Microsoft.Extensions.Logging;

namespace Oppo;

public sealed class MagnetarClient(string hostName, string macAddress, ILogger<MagnetarClient> logger) : IOppoClient
{
    private const int Port = 8102;
    private readonly ILogger<MagnetarClient> _logger = logger;
    private readonly string _hostName = hostName;
    private readonly IPAddress _ipAddress = IPAddress.Parse(hostName);
    private readonly string _macAddress = macAddress;

    private readonly TcpClient _tcpClient = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly TimeSpan _timeout = TimeSpan.FromSeconds(1);

    public string HostName => _hostName;


    private PowerState _lastPowerState = PowerState.Off;

    public async ValueTask<OppoResult<PowerState>> PowerToggleAsync(CancellationToken cancellationToken = default)
    {
        if (_lastPowerState == PowerState.Off)
            await WakeOnLan.SendWakeOnLanAsync(_ipAddress, _macAddress);

        var result = await SendCommand("#POW", cancellationToken);
        if (!result.Success)
            return false;
        _lastPowerState = _lastPowerState switch
        {
            PowerState.Off => PowerState.On,
            PowerState.On => PowerState.Off,
            _ => _lastPowerState
        };
        return new OppoResult<PowerState>
        {
            Success = true,
            Result = _lastPowerState
        };
    }

    public async ValueTask<OppoResult<PowerState>> PowerOnAsync(CancellationToken cancellationToken = default)
    {
        await WakeOnLan.SendWakeOnLanAsync(_ipAddress, _macAddress);
        var result = await SendCommand("#PON", cancellationToken);
        if (!result.Success)
            return false;

        _lastPowerState = PowerState.On;
        return new OppoResult<PowerState>
        {
            Success = true,
            Result = PowerState.On
        };
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
    private async ValueTask<OppoResultCore> SendCommand(string command, CancellationToken cancellationToken)
    {
        if (!await _semaphore.WaitAsync(_timeout, cancellationToken))
            return OppoResultCore.FalseResult;

        try
        {
            if (_logger.IsEnabled(LogLevel.Trace))
                _logger.SendingCommand(command);

            await IsConnectedAsync();

            var networkStream = _tcpClient.GetStream();
            await networkStream.WriteAsync(Encoding.ASCII.GetBytes(command), cancellationToken);
            await networkStream.WriteAsync(CarriageReturnLineFeed, cancellationToken);

            return OppoResultCore.SuccessResult("ack");
        }
        catch (Exception e)
        {
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
        _tcpClient.Dispose();
        _semaphore.Dispose();
    }
}