using System.Buffers;
using System.ComponentModel.DataAnnotations;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Channels;

using Microsoft.Extensions.Logging;

namespace Oppo;

public sealed class OppoClient(string hostName, in OppoModel model, ILogger<OppoClient> logger) : IOppoClient
{
    private readonly string _hostName = hostName;
    private readonly OppoModel _model = model;
    private readonly ILogger<OppoClient> _logger = logger;
    
    private readonly bool _is20XModel = model is OppoModel.UDP203 or OppoModel.UDP205;
    private readonly ushort _port = model switch
    {
        OppoModel.BDP83 => 19999,
        OppoModel.BDP9X or OppoModel.BDP10X => 48360,
        OppoModel.UDP203 or OppoModel.UDP205 => 23,
        _ => throw new InvalidOperationException($"Model {model} is not supported.")
    };

    private TcpClient _tcpClient = ConnectHelper.CreateTcpClient();
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly TimeSpan _timeout = TimeSpan.FromSeconds(1);

    private const string OkOn = "@OK ON";
    private const string OkOff = "@OK OFF";

    private readonly Lock _streamingSync = new();
    private CancellationTokenSource? _readerCts;
    private Task? _readerTask;
    private PendingCommandResponse? _pendingCommandResponse;
    private Channel<OppoStreamingEvent>? _streamingChannel;

    internal bool IsDisposed { get; private set; }

    public async ValueTask<OppoResult<PowerState>> PowerToggleAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommandWithRetry(
            _is20XModel ? Oppo20XCommand.PowerToggle : Oppo10XCommand.PowerToggle,
            cancellationToken);
        
        return result.Success switch
        {
            false => false,
            _ => new OppoResult<PowerState>
            {
                Success = true,
                Result = result.Response switch
                {
                    OkOn => PowerState.On,
                    OkOff => PowerState.Off,
                    _ => LogError(result.Response, PowerState.Unknown)
                }
            }
        };
    }

    public async ValueTask<OppoResult<PowerState>> PowerOnAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommandWithRetry(
            _is20XModel ? Oppo20XCommand.PowerOn : Oppo10XCommand.PowerOn,
            cancellationToken);

        return result.Success switch
        {
            false => false,
            _ => new OppoResult<PowerState>
            {
                Success = true,
                Result = result.Response switch
                {
                    OkOn => PowerState.On,
                    _ => LogError(result.Response, PowerState.Unknown)
                }
            }
        };
    }

    public async ValueTask<OppoResult<PowerState>> PowerOffAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommandWithRetry(
            _is20XModel ? Oppo20XCommand.PowerOff : Oppo10XCommand.PowerOff,
            cancellationToken);

        return result.Success switch
        {
            false => false,
            _ => new OppoResult<PowerState>
            {
                Success = true,
                Result = result.Response switch
                {
                    OkOff => PowerState.Off,
                    _ => LogError(result.Response, PowerState.Unknown)
                }
            }
        };
    }

    public async ValueTask<OppoResult<TrayState>> EjectToggleAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommandWithRetry(
            _is20XModel ? Oppo20XCommand.EjectToggle : Oppo10XCommand.EjectToggle,
            cancellationToken);

        return result.Success switch
        {
            false => false,
            _ => new OppoResult<TrayState>
            {
                Success = true,
                Result = result.Response switch
                {
                    "@OK OPEN" => TrayState.Open,
                    "@OK CLOSE" => TrayState.Closed,
                    _ => LogError(result.Response, TrayState.Unknown)
                }
            }
        };
    }

    public async ValueTask<OppoResult<DimmerState>> DimmerAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommandWithRetry(
            _is20XModel ? Oppo20XCommand.Dimmer : Oppo10XCommand.Dimmer,
            cancellationToken);

        return result.Success switch
        {
            false => false,
            _ => new OppoResult<DimmerState>
            {
                Success = true,
                Result = result.Response switch
                {
                    OkOn => DimmerState.On,
                    "@OK DIM" => DimmerState.Dim,
                    OkOff => DimmerState.Off,
                    _ => LogError(result.Response, DimmerState.Unknown)
                }
            }
        };
    }

    public async ValueTask<OppoResult<PureAudioState>> PureAudioToggleAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommandWithRetry(
            _is20XModel ? Oppo20XCommand.PureAudioToggle : Oppo10XCommand.PureAudioToggle,
            cancellationToken);

        return result.Success switch
        {
            false => false,
            _ => new OppoResult<PureAudioState>
            {
                Success = true,
                Result = result.Response switch
                {
                    OkOn => PureAudioState.On,
                    OkOff => PureAudioState.Off,
                    _ => LogError(result.Response, PureAudioState.Unknown)
                }
            }
        };
    }

    public async ValueTask<OppoResult<ushort?>> VolumeUpAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommandWithRetry(
            _is20XModel ? Oppo20XCommand.VolumeUp : Oppo10XCommand.VolumeUp,
            cancellationToken);

        return result.Success switch
        {
            false => false,
            _ => new OppoResult<ushort?>
            {
                Success = ushort.TryParse(result.Response.AsSpan()[4..], out var volume),
                Result = volume
            }
        };
    }
    
    public async ValueTask<OppoResult<ushort?>> VolumeDownAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommandWithRetry(
            _is20XModel ? Oppo20XCommand.VolumeDown : Oppo10XCommand.VolumeDown,
            cancellationToken);

        return result.Success switch
        {
            false => false,
            _ => new OppoResult<ushort?>
            {
                Success = ushort.TryParse(result.Response.AsSpan()[4..], out var volume),
                Result = volume
            }
        };
    }
    
    public async ValueTask<OppoResult<MuteState>> MuteToggleAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommandWithRetry(
            _is20XModel ? Oppo20XCommand.MuteToggle : Oppo10XCommand.MuteToggle,
            cancellationToken);

        return result.Success switch
        {
            false => false,
            _ => new OppoResult<MuteState>
            {
                Success = true,
                Result = result.Response switch
                {
                    "@OK MUTE" => MuteState.On,
                    "@OK UNMUTE" => MuteState.Off,
                    _ => LogError(result.Response, MuteState.Unknown)
                }
            }
        };
    }
    
    public async ValueTask<bool> NumericInputAsync([Range(0, 9)] ushort number, CancellationToken cancellationToken = default)
    {
        if (number > 9)
            return false;

        return (await SendCommandWithRetry(
                number switch
                {
                    0 => _is20XModel ? Oppo20XCommand.NumericKey0 : Oppo10XCommand.NumericKey0,
                    1 => _is20XModel ? Oppo20XCommand.NumericKey1 : Oppo10XCommand.NumericKey1,
                    2 => _is20XModel ? Oppo20XCommand.NumericKey2 : Oppo10XCommand.NumericKey2,
                    3 => _is20XModel ? Oppo20XCommand.NumericKey3 : Oppo10XCommand.NumericKey3,
                    4 => _is20XModel ? Oppo20XCommand.NumericKey4 : Oppo10XCommand.NumericKey4,
                    5 => _is20XModel ? Oppo20XCommand.NumericKey5 : Oppo10XCommand.NumericKey5,
                    6 => _is20XModel ? Oppo20XCommand.NumericKey6 : Oppo10XCommand.NumericKey6,
                    7 => _is20XModel ? Oppo20XCommand.NumericKey7 : Oppo10XCommand.NumericKey7,
                    8 => _is20XModel ? Oppo20XCommand.NumericKey8 : Oppo10XCommand.NumericKey8,
                    9 => _is20XModel ? Oppo20XCommand.NumericKey9 : Oppo10XCommand.NumericKey9,
                    _ => throw new ArgumentOutOfRangeException(nameof(number))
                },
                cancellationToken)
            ).Success;
    }

    public async ValueTask<bool> ClearAsync(CancellationToken cancellationToken = default) =>
        (await SendCommandWithRetry(_is20XModel ? Oppo20XCommand.Clear : Oppo10XCommand.Clear, cancellationToken)).Success;

    public async ValueTask<bool> GoToAsync(CancellationToken cancellationToken = default) =>
        (await SendCommandWithRetry(_is20XModel ? Oppo20XCommand.GoTo : Oppo10XCommand.GoTo, cancellationToken)).Success;

    public async ValueTask<bool> HomeAsync(CancellationToken cancellationToken = default) =>
        (await SendCommandWithRetry(_is20XModel ? Oppo20XCommand.Home : Oppo10XCommand.Home, cancellationToken)).Success;

    public async ValueTask<bool> PageUpAsync(CancellationToken cancellationToken = default) =>
        (await SendCommandWithRetry(_is20XModel ? Oppo20XCommand.PageUp : Oppo10XCommand.PageUp, cancellationToken)).Success;

    public async ValueTask<bool> PageDownAsync(CancellationToken cancellationToken = default) =>
        (await SendCommandWithRetry(_is20XModel ? Oppo20XCommand.PageDown : Oppo10XCommand.PageDown, cancellationToken)).Success;

    public async ValueTask<bool> InfoToggleAsync(CancellationToken cancellationToken = default) =>
        (await SendCommandWithRetry(_is20XModel ? Oppo20XCommand.InfoToggle : Oppo10XCommand.InfoToggle, cancellationToken)).Success;

    public async ValueTask<bool> TopMenuAsync(CancellationToken cancellationToken = default) =>
        (await SendCommandWithRetry(_is20XModel ? Oppo20XCommand.TopMenu : Oppo10XCommand.TopMenu, cancellationToken)).Success;

    public async ValueTask<bool> PopUpMenuAsync(CancellationToken cancellationToken = default) =>
        (await SendCommandWithRetry(_is20XModel ? Oppo20XCommand.PopUpMenu : Oppo10XCommand.PopUpMenu, cancellationToken)).Success;

    public async ValueTask<bool> UpArrowAsync(CancellationToken cancellationToken = default) =>
        (await SendCommandWithRetry(_is20XModel ? Oppo20XCommand.UpArrow : Oppo10XCommand.UpArrow, cancellationToken)).Success;

    public async ValueTask<bool> LeftArrowAsync(CancellationToken cancellationToken = default) =>
        (await SendCommandWithRetry(_is20XModel ? Oppo20XCommand.LeftArrow : Oppo10XCommand.LeftArrow, cancellationToken)).Success;

    public async ValueTask<bool> RightArrowAsync(CancellationToken cancellationToken = default) =>
        (await SendCommandWithRetry(_is20XModel ? Oppo20XCommand.RightArrow : Oppo10XCommand.RightArrow, cancellationToken)).Success;

    public async ValueTask<bool> DownArrowAsync(CancellationToken cancellationToken = default) =>
        (await SendCommandWithRetry(_is20XModel ? Oppo20XCommand.DownArrow : Oppo10XCommand.DownArrow, cancellationToken)).Success;

    public async ValueTask<bool> EnterAsync(CancellationToken cancellationToken = default) =>
        (await SendCommandWithRetry(_is20XModel ? Oppo20XCommand.Enter : Oppo10XCommand.Enter, cancellationToken)).Success;

    public async ValueTask<bool> SetupAsync(CancellationToken cancellationToken = default) =>
        (await SendCommandWithRetry(_is20XModel ? Oppo20XCommand.Setup : Oppo10XCommand.Setup, cancellationToken)).Success;

    public async ValueTask<bool> ReturnAsync(CancellationToken cancellationToken = default) =>
        (await SendCommandWithRetry(_is20XModel ? Oppo20XCommand.Return : Oppo10XCommand.Return, cancellationToken)).Success;

    public async ValueTask<bool> RedAsync(CancellationToken cancellationToken = default) =>
        (await SendCommandWithRetry(_is20XModel ? Oppo20XCommand.Red : Oppo10XCommand.Red, cancellationToken)).Success;

    public async ValueTask<bool> GreenAsync(CancellationToken cancellationToken = default) =>
        (await SendCommandWithRetry(_is20XModel ? Oppo20XCommand.Green : Oppo10XCommand.Green, cancellationToken)).Success;

    public async ValueTask<bool> BlueAsync(CancellationToken cancellationToken = default) =>
        (await SendCommandWithRetry(_is20XModel ? Oppo20XCommand.Blue : Oppo10XCommand.Blue, cancellationToken)).Success;

    public async ValueTask<bool> YellowAsync(CancellationToken cancellationToken = default) =>
        (await SendCommandWithRetry(_is20XModel ? Oppo20XCommand.Yellow : Oppo10XCommand.Yellow, cancellationToken)).Success;

    public async ValueTask<bool> StopAsync(CancellationToken cancellationToken = default) =>
        (await SendCommandWithRetry(_is20XModel ? Oppo20XCommand.Stop : Oppo10XCommand.Stop, cancellationToken)).Success;

    public async ValueTask<bool> PlayAsync(CancellationToken cancellationToken = default) =>
        (await SendCommandWithRetry(_is20XModel ? Oppo20XCommand.Play : Oppo10XCommand.Play, cancellationToken)).Success;

    public async ValueTask<bool> PauseAsync(CancellationToken cancellationToken = default) =>
        (await SendCommandWithRetry(_is20XModel ? Oppo20XCommand.Pause : Oppo10XCommand.Pause, cancellationToken)).Success;

    public async ValueTask<bool> PreviousAsync(CancellationToken cancellationToken = default) =>
        (await SendCommandWithRetry(_is20XModel ? Oppo20XCommand.Previous : Oppo10XCommand.Previous, cancellationToken)).Success;

    public async ValueTask<OppoResult<ushort?>> ReverseAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommandWithRetry(
            _is20XModel ? Oppo20XCommand.Reverse : Oppo10XCommand.Reverse,
            cancellationToken);
        
        return result.Success switch
        {
            false => false,
            _ => new OppoResult<ushort?>
            {
                Success = ushort.TryParse(result.Response.AsSpan()[4..^1], out var speed),
                Result = speed
            }
        };
    }
    
    public async ValueTask<OppoResult<ushort?>> ForwardAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommandWithRetry(
            _is20XModel ? Oppo20XCommand.Forward : Oppo10XCommand.Forward,
            cancellationToken);
        
        return result.Success switch
        {
            false => false,
            _ => new OppoResult<ushort?>
            {
                Success = ushort.TryParse(result.Response.AsSpan()[4..^1], out var speed),
                Result = speed
            }
        };
    }
    
    public async ValueTask<bool> NextAsync(CancellationToken cancellationToken = default) =>
        (await SendCommandWithRetry(_is20XModel ? Oppo20XCommand.Next : Oppo10XCommand.Next, cancellationToken)).Success;

    public async ValueTask<bool> AudioAsync(CancellationToken cancellationToken = default) =>
        (await SendCommandWithRetry(_is20XModel ? Oppo20XCommand.Audio : Oppo10XCommand.Audio, cancellationToken)).Success;

    public async ValueTask<bool> SubtitleAsync(CancellationToken cancellationToken = default) =>
        (await SendCommandWithRetry(_is20XModel ? Oppo20XCommand.Subtitle : Oppo10XCommand.Subtitle, cancellationToken)).Success;

    public async ValueTask<OppoResult<string>> AngleAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommandWithRetry(
            _is20XModel ? Oppo20XCommand.Angle : Oppo10XCommand.Angle,
            cancellationToken);
        
        return result.Success switch
        {
            false => false,
            _ => new OppoResult<string>
            {
                Success = true,
                Result = result.Response[4..]
            }
        };
    }
    
    public async ValueTask<OppoResult<string>> ZoomAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommandWithRetry(
            _is20XModel ? Oppo20XCommand.Zoom : Oppo10XCommand.Zoom,
            cancellationToken);
        
        return result.Success switch
        {
            false => false,
            _ => new OppoResult<string>
            {
                Success = true,
                Result = result.Response[4..]
            }
        };
    }
    
    public async ValueTask<OppoResult<string>> SecondaryAudioProgramAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommandWithRetry(
            _is20XModel ? Oppo20XCommand.SecondaryAudioProgram : Oppo10XCommand.SecondaryAudioProgram,
            cancellationToken);
        
        return result.Success switch
        {
            false => false,
            _ => new OppoResult<string>
            {
                Success = true,
                Result = result.Response[4..]
            }
        };
    }
    
    public async ValueTask<OppoResult<ABReplayState>> ABReplayAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommandWithRetry(
            _is20XModel ? Oppo20XCommand.ABReplay : Oppo10XCommand.ABReplay,
            cancellationToken);
        
        return result.Success switch
        {
            false => false,
            _ => new OppoResult<ABReplayState>
            {
                Success = true,
                Result = result.Response switch
                {
                    "@OK A-" => ABReplayState.A,
                    "@OK AB" => ABReplayState.AB,
                    OkOff => ABReplayState.Off,
                    _ => LogError(result.Response, ABReplayState.Unknown)
                }
            }
        };
    }
    
    public async ValueTask<OppoResult<RepeatState>> RepeatAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommandWithRetry(
            _is20XModel ? Oppo20XCommand.Repeat : Oppo10XCommand.Repeat,
            cancellationToken);
        
        return result.Success switch
        {
            false => false,
            _ => new OppoResult<RepeatState>
            {
                Success = true,
                Result = result.Response switch
                {
                    "@OK Repeat Chapter" => RepeatState.RepeatChapter,
                    "@OK Repeat Title" => RepeatState.RepeatTitle,
                    OkOff => RepeatState.Off,
                    _ => LogError(result.Response, RepeatState.Unknown)
                }
            }
        };
    }
    
    public async ValueTask<OppoResult<string>> PictureInPictureAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommandWithRetry(
            _is20XModel ? Oppo20XCommand.PictureInPicture : Oppo10XCommand.PictureInPicture,
            cancellationToken);
        
        return result.Success switch
        {
            false => false,
            _ => new OppoResult<string>
            {
                Success = true,
                Result = result.Response[4..]
            }
        };
    }
    
    public async ValueTask<bool> ResolutionAsync(CancellationToken cancellationToken = default) =>
        (await SendCommandWithRetry(_is20XModel ? Oppo20XCommand.Resolution : Oppo10XCommand.Resolution, cancellationToken)).Success;

    public async ValueTask<bool> SubtitleHoldAsync(CancellationToken cancellationToken = default) =>
        (await SendCommandWithRetry(_is20XModel ? Oppo20XCommand.SubtitleHold : Oppo10XCommand.SubtitleHold, cancellationToken)).Success;

    public async ValueTask<bool> OptionAsync(CancellationToken cancellationToken = default) =>
        _model is not OppoModel.BDP83 and not OppoModel.BDP9X &&
        (await SendCommandWithRetry(_is20XModel ? Oppo20XCommand.Option : Oppo10XCommand.Option, cancellationToken)).Success;

    public async ValueTask<bool> ThreeDAsync(CancellationToken cancellationToken = default) =>
        _model is not OppoModel.BDP83 and not OppoModel.BDP9X &&
        (await SendCommandWithRetry(_is20XModel ? Oppo20XCommand.ThreeD : Oppo10XCommand.ThreeD, cancellationToken)).Success;

    public async ValueTask<bool> PictureAdjustmentAsync(CancellationToken cancellationToken = default) =>
        _model is not OppoModel.BDP83 and not OppoModel.BDP9X &&
        (await SendCommandWithRetry(_is20XModel ? Oppo20XCommand.PictureAdjustment : Oppo10XCommand.PictureAdjustment, cancellationToken)).Success;

    public async ValueTask<bool> HDRAsync(CancellationToken cancellationToken = default) =>
        _is20XModel &&
        (await SendCommandWithRetry(Oppo20XCommand.HDR, cancellationToken)).Success;

    public async ValueTask<bool> InfoHoldAsync(CancellationToken cancellationToken = default) =>
        _is20XModel &&
        (await SendCommandWithRetry(Oppo20XCommand.InfoHold, cancellationToken)).Success;

    public async ValueTask<bool> ResolutionHoldAsync(CancellationToken cancellationToken = default) =>
        _is20XModel &&
        (await SendCommandWithRetry(Oppo20XCommand.ResolutionHold, cancellationToken)).Success;

    public async ValueTask<bool> AVSyncAsync(CancellationToken cancellationToken = default) =>
        _is20XModel &&
        (await SendCommandWithRetry(Oppo20XCommand.AVSync, cancellationToken)).Success;

    public async ValueTask<bool> GaplessPlayAsync(CancellationToken cancellationToken = default) =>
        _is20XModel && (await SendCommandWithRetry(Oppo20XCommand.GaplessPlay, cancellationToken)).Success;

    public async ValueTask<bool> NoopAsync(CancellationToken cancellationToken = default) =>
        (await SendCommandWithRetry(_is20XModel ? Oppo20XCommand.Noop : Oppo10XCommand.Noop, cancellationToken)).Success;

    public async ValueTask<bool> InputAsync(CancellationToken cancellationToken = default) =>
        (await SendCommandWithRetry(_is20XModel ? Oppo20XCommand.Input : Oppo10XCommand.Input, cancellationToken)).Success;

    public async ValueTask<OppoResult<RepeatMode>> SetRepeatAsync(RepeatMode mode, CancellationToken cancellationToken = default)
    {
        if (mode == RepeatMode.Unknown)
            return false;
        
        var result = await SendCommandWithRetry(mode switch
        {
            RepeatMode.Chapter => _is20XModel ? Oppo20XAdvancedCommand.SetRepeatModeChapter : Oppo10XAdvancedCommand.SetRepeatModeChapter,
            RepeatMode.Title => _is20XModel ? Oppo20XAdvancedCommand.SetRepeatModeTitle : Oppo10XAdvancedCommand.SetRepeatModeTitle,
            RepeatMode.All => _is20XModel ? Oppo20XAdvancedCommand.SetRepeatModeAll : Oppo10XAdvancedCommand.SetRepeatModeAll,
            RepeatMode.Off => _is20XModel ? Oppo20XAdvancedCommand.SetRepeatModeOff : Oppo10XAdvancedCommand.SetRepeatModeOff,
            RepeatMode.Shuffle => _is20XModel ? Oppo20XAdvancedCommand.SetRepeatModeShuffle : Oppo10XAdvancedCommand.SetRepeatModeShuffle,
            RepeatMode.Random => _is20XModel ? Oppo20XAdvancedCommand.SetRepeatModeRandom : Oppo10XAdvancedCommand.SetRepeatModeRandom,
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown repeat mode")
        }, cancellationToken);
        
        return result.Success switch
        {
            false => false,
            _ => new OppoResult<RepeatMode>
            {
                Success = true,
                Result = result.Response switch
                {
                    "@OK CH" => RepeatMode.Chapter,
                    "@OK TT" => RepeatMode.Title,
                    "@OK ALL" => RepeatMode.All,
                    OkOff => RepeatMode.Off,
                    "@OK SHF" => RepeatMode.Shuffle,
                    "@OK RND" => RepeatMode.Random,
                    _ => LogError(result.Response, RepeatMode.Unknown)
                }
            }
        };
    }

    public async ValueTask<OppoResult<ushort>> SetVolumeAsync([Range(0, 100)] ushort volume, CancellationToken cancellationToken = default)
    {
        if (volume > 100)
            return false;
        
        var command = Encoding.ASCII.GetBytes(_is20XModel ? $"#SVL {volume}\r" : $"REMOTE SVL {volume}");
        var result = await SendCommandWithRetry(command, cancellationToken);
        
        return result.Success switch
        {
            false => false,
            _ => new OppoResult<ushort>
            {
                Success = ushort.TryParse(result.Response.AsSpan()[4..], out var newVolume),
                Result = newVolume
            }
        };
    }

    public async ValueTask<OppoResult<VolumeInfo>> QueryVolumeAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommandWithRetry(_is20XModel ? Oppo20XQueryCommand.QueryVolume : Oppo10XQueryCommand.QueryVolume, cancellationToken);

        if (!result.Success)
            return false;

        bool muted;
        ushort? volume;
        if (result.Response.Equals("@OK MUTE", StringComparison.Ordinal))
        {
            muted = true;
            volume = null;
        }
        else
        {
            muted = false;
            volume = ushort.TryParse(result.Response.AsSpan()[4..], out var newVolume) ? newVolume : null;
        }

        return new OppoResult<VolumeInfo>
        {
            Success = true,
            Result = new VolumeInfo(volume, muted)
        };
    }

    public async ValueTask<OppoResult<PowerState>> QueryPowerStatusAsync(CancellationToken cancellationToken = default)
    {
        var command = _is20XModel ? Oppo20XQueryCommand.QueryPowerStatus : Oppo10XQueryCommand.QueryPowerStatus;
        var result = await SendCommandWithRetry(command, cancellationToken);
        
        return result.Success switch
        {
            false => false,
            _ => new OppoResult<PowerState>
            {
                Success = true,
                Result = result.Response switch
                {
                    OkOn => PowerState.On,
                    OkOff => PowerState.Off,
                    _ => LogError(result.Response, PowerState.Unknown)
                }
            }
        };
    }

    public async ValueTask<OppoResult<PlaybackStatus>> QueryPlaybackStatusAsync(CancellationToken cancellationToken = default)
    {
        var command = _is20XModel ? Oppo20XQueryCommand.QueryPlaybackStatus : Oppo10XQueryCommand.QueryPlaybackStatus;
        var result = await SendCommandWithRetry(command, cancellationToken);
        
        return result.Success switch
        {
            false => false,
            _ => new OppoResult<PlaybackStatus>
            {
                Success = true,
                Result = result.Response switch
                {
                    "@OK PLAY" => PlaybackStatus.Play,
                    "@OK PAUSE" => PlaybackStatus.Pause,
                    "@OK STOP" => PlaybackStatus.Stop,
                    "@OK STEP" => PlaybackStatus.Step,
                    "@OK FREV" => PlaybackStatus.FastRewind,
                    "@OK FFWD" => PlaybackStatus.FastForward,
                    "@OK SFWD" => PlaybackStatus.SlowForward,
                    "@OK SREV" => PlaybackStatus.SlowRewind,
                    "@OK SETUP" => PlaybackStatus.Setup,
                    "@OK HOME MENU" => PlaybackStatus.HomeMenu,
                    "@OK MEDIA CENTER" => PlaybackStatus.MediaCenter,
                    "@OK SCREEN SAVER" => PlaybackStatus.ScreenSaver,
                    "@OK DISC MENU" => PlaybackStatus.DiscMenu,
                        
                    // Pre 20X models
                    "@OK NO DISC" => PlaybackStatus.NoDisc,
                    "@OK LOADING" => PlaybackStatus.Loading,
                    "@OK OPEN" => PlaybackStatus.Open,
                    "OK CLOSE" or "@OK CLOSE" => PlaybackStatus.Close,
                    "@OK UNKNOW" => PlaybackStatus.Unknown,
                    _ => LogError(result.Response, PlaybackStatus.Unknown)
                }
            }
        };
    }

    public async ValueTask<OppoResult<HDMIResolution>> QueryHDMIResolutionAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommandWithRetry(_is20XModel ? Oppo20XQueryCommand.QueryHDMIResolution : Oppo10XQueryCommand.QueryHDMIResolution,
            cancellationToken);

        return result.Success switch
        {
            false => false,
            _ => new OppoResult<HDMIResolution>
            {
                Success = true,
                Result = result.Response switch
                {
                    "@OK 480I" => HDMIResolution.R480i,
                    "@OK 480P" => HDMIResolution.R480p,
                    "@OK 576I" => HDMIResolution.R576i,
                    "@OK 576P" => HDMIResolution.R576p,
                    "@OK 720P50" => HDMIResolution.R720p50,
                    "@OK 720P60" => HDMIResolution.R720p60,
                    "@OK 1080I50" => HDMIResolution.R1080i50,
                    "@OK 1080I60" => HDMIResolution.R1080i60,
                    "@OK 1080P24" => HDMIResolution.R1080p24,
                    "@OK 1080P50" => HDMIResolution.R1080p50,
                    "@OK 1080P60" => HDMIResolution.R1080p60,
                    "@OK 1080PAUTO" => HDMIResolution.R1080PAuto,
                    "@OK UHD24" => HDMIResolution.RUltraHDp24,
                    "@OK UHD50" => HDMIResolution.RUltraHDp50,
                    "@OK UHD60" => HDMIResolution.RUltraHDp60,
                    "@OK UHD_AUTO" => HDMIResolution.RUltraHDAuto,
                    "@OK AUTO" => HDMIResolution.Auto,
                    "@OK Source Direct" => HDMIResolution.SourceDirect,
                    _ => LogError(result.Response, HDMIResolution.Unknown)
                }
            }
        };
    }

    public ValueTask<OppoResult<uint>> QueryTrackOrTitleElapsedTimeAsync(CancellationToken cancellationToken = default)
        => QueryTimeAsync(_is20XModel ? Oppo20XQueryCommand.QueryTrackOrTitleElapsedTime : Oppo10XQueryCommand.QueryTrackOrTitleElapsedTime,
            cancellationToken);

    public ValueTask<OppoResult<uint>> QueryTrackOrTitleRemainingTimeAsync(CancellationToken cancellationToken = default)
        => QueryTimeAsync(_is20XModel ? Oppo20XQueryCommand.QueryTrackOrTitleRemainingTime : Oppo10XQueryCommand.QueryTrackOrTitleRemainingTime,
            cancellationToken);

    public ValueTask<OppoResult<uint>> QueryChapterElapsedTimeAsync(CancellationToken cancellationToken = default)
        => QueryTimeAsync(_is20XModel ? Oppo20XQueryCommand.QueryChapterElapsedTime : Oppo10XQueryCommand.QueryChapterElapsedTime,
            cancellationToken);

    public ValueTask<OppoResult<uint>> QueryChapterRemainingTimeAsync(CancellationToken cancellationToken = default)
        => QueryTimeAsync(_is20XModel ? Oppo20XQueryCommand.QueryChapterRemainingTime : Oppo10XQueryCommand.QueryChapterRemainingTime,
            cancellationToken);

    public ValueTask<OppoResult<uint>> QueryTotalElapsedTimeAsync(CancellationToken cancellationToken = default)
        => QueryTimeAsync(_is20XModel ? Oppo20XQueryCommand.QueryTotalElapsedTime : Oppo10XQueryCommand.QueryTotalElapsedTime,
            cancellationToken);

    public ValueTask<OppoResult<uint>> QueryTotalRemainingTimeAsync(CancellationToken cancellationToken = default)
        => QueryTimeAsync(_is20XModel ? Oppo20XQueryCommand.QueryTotalRemainingTime : Oppo10XQueryCommand.QueryTotalRemainingTime,
            cancellationToken);

    public async ValueTask<OppoResult<DiscType>> QueryDiscTypeAsync(CancellationToken cancellationToken = default)
    {
        var command = _is20XModel ? Oppo20XQueryCommand.QueryDiscType : Oppo10XQueryCommand.QueryDiscType;
        var result = await SendCommandWithRetry(command, cancellationToken);
        
        return result.Success switch
        {
            false => false,
            _ => new OppoResult<DiscType>
            {
                Success = true,
                Result = result.Response switch
                {
                    "@OK BD-MV" => DiscType.BlueRayMovie,
                    "@OK DVD-AUDIO" => DiscType.DVDAudio,
                    "@OK DVD-VIDEO" => DiscType.DVDVideo,
                    "@OK SACD" => DiscType.SACD,
                    "@OK CDDA" => DiscType.CDDiscAudio,
                    "@OK DATA-DISC" => DiscType.DataDisc,
                    "@OK UHBD" => DiscType.UltraHDBluRay,
                    "@OK NO-DISC" => DiscType.NoDisc,
                    "@OK UNKNOW-DISC" => DiscType.UnknownDisc,
                        
                    // Pre 20X models
                    "@OK HDCD" => DiscType.HDCD,
                    _ => LogError(result.Response, DiscType.Unknown)
                }
            }
        };
    }

    public async ValueTask<OppoResult<string>> QueryAudioTypeAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommandWithRetry(_is20XModel ? Oppo20XQueryCommand.QueryAudioType : Oppo10XQueryCommand.QueryAudioType, cancellationToken);

        return result.Success switch
        {
            false => false,
            _ => new OppoResult<string>
            {
                Success = true,
                Result = result.Response[4..]
            }
        };
    }

    public async ValueTask<OppoResult<string>> QuerySubtitleTypeAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommandWithRetry(_is20XModel ? Oppo20XQueryCommand.QuerySubtitleType : Oppo10XQueryCommand.QuerySubtitleType,
            cancellationToken);

        return result.Success switch
        {
            false => false,
            _ => new OppoResult<string>
            {
                Success = true,
                Result = result.Response[4..]
            }
        };
    }

    public async ValueTask<OppoResult<bool>> QueryThreeDStatusAsync(CancellationToken cancellationToken = default)
    {
        if (!_is20XModel)
            return false;

        var result = await SendCommandWithRetry(Oppo20XQueryCommand.QueryThreeDStatus, cancellationToken);

        return result.Success switch
        {
            false => false,
            _ => new OppoResult<bool>
            {
                Success = true,
                Result = result.Response switch
                {
                    "@OK 2D" => false,
                    "@OK 3D" => true,
                    _ => false
                }
            }
        };
    }

    public async ValueTask<OppoResult<HDRStatus>> QueryHDRStatusAsync(CancellationToken cancellationToken = default)
    {
        if (!_is20XModel)
            return false;

        var result = await SendCommandWithRetry(Oppo20XQueryCommand.QueryHDRStatus, cancellationToken);

        return result.Success switch
        {
            false => false,
            _ => new OppoResult<HDRStatus>
            {
                Success = true,
                Result = result.Response switch
                {
                    "@OK HDR" => HDRStatus.HDR,
                    "@OK SDR" => HDRStatus.SDR,
                    "@OK DOV" => HDRStatus.DolbyVision,
                    _ => LogError(result.Response, HDRStatus.Unknown)
                }
            }
        };
    }

    public async ValueTask<OppoResult<AspectRatio>> QueryAspectRatioAsync(CancellationToken cancellationToken = default)
    {
        if (!_is20XModel)
            return false;

        var result = await SendCommandWithRetry(Oppo20XQueryCommand.QueryAspectRatio, cancellationToken);

        return result.Success switch
        {
            false => false,
            _ => new OppoResult<AspectRatio>
            {
                Success = true,
                Result = result.Response switch
                {
                    "@OK 16WW" => AspectRatio.A16WW,
                    "@OK 16AW" => AspectRatio.A16AW,
                    "@OK 16A4" => AspectRatio.A169A,
                    "@OK 21M0" => AspectRatio.A21M0,
                    "@OK 21M1" => AspectRatio.A21M1,
                    "@OK 21M2" => AspectRatio.A21M2,
                    "@OK 21F0" => AspectRatio.A21F0,
                    "@OK 21F1" => AspectRatio.A21F1,
                    "@OK 21F2" => AspectRatio.A21F2,
                    "@OK 21C0" => AspectRatio.A21C0,
                    "@OK 21C1" => AspectRatio.A21C1,
                    "@OK 21C2" => AspectRatio.A21C2,
                    _ => LogError(result.Response, AspectRatio.Unknown)
                }
            }
        };
    }

    public async ValueTask<OppoResult<CurrentRepeatMode>> QueryRepeatModeAsync(CancellationToken cancellationToken = default)
    {
        var command = _is20XModel ? Oppo20XQueryCommand.QueryRepeatMode : Oppo10XQueryCommand.QueryRepeatMode;
        var result = await SendCommandWithRetry(command, cancellationToken);
        
        return result.Success switch
        {
            false => false,
            _ => new OppoResult<CurrentRepeatMode>
            {
                Success = true,
                Result = result.Response switch
                {
                    "@OK 00 Off" => CurrentRepeatMode.Off,
                    "@OK 01 Repeat One" => CurrentRepeatMode.RepeatOne,
                    "@OK 02 Repeat Chapter" => CurrentRepeatMode.RepeatChapter,
                    "@OK 03 Repeat All" => CurrentRepeatMode.RepeatAll,
                    "@OK 04 Repeat Title" => CurrentRepeatMode.RepeatTitle,
                    "@OK 05 Shuffle" => CurrentRepeatMode.Shuffle,
                    "@OK 06 Random" => CurrentRepeatMode.Random,
                    _ => LogError(result.Response, CurrentRepeatMode.Unknown)
                }
            }
        };
    }

    public async ValueTask<OppoResult<InputSource>> QueryInputSourceAsync(CancellationToken cancellationToken = default)
    {
        if (_model is OppoModel.BDP83 or OppoModel.BDP9X)
            return false;
        
        var result = await SendCommandWithRetry(_is20XModel ? Oppo20XQueryCommand.QueryInputSource : Oppo10XQueryCommand.QueryInputSource,
            cancellationToken);
        
        return result.Success switch
        {
            false => false,
            _ => new OppoResult<InputSource>
            {
                Success = true,
                Result = result.Response switch
                {
                    "@OK 0 BD-PLAYER" => InputSource.BluRayPlayer,
                    "@OK 1 HDMI-IN" => InputSource.HDMIIn,
                    "@OK 2 ARC-HDMI-OUT" => InputSource.ARCHDMIOut,
                    "@OK 3 OPTICAL-IN" => InputSource.Optical,
                    "@OK 4 COAXIAL-IN" => InputSource.Coaxial,
                    "@OK 5 USB-AUDIO-IN" => InputSource.USBAudio,
                    "@OK 1 HDMI-FRONT" => InputSource.HDMIFront,
                    "@OK 2 HDMI-BACK" => InputSource.HDMIBack,
                    "@OK 3 ARC-HDMI-OUT1" => InputSource.ARCHDMIOut1,
                    "@OK 4 ARC-HDMI-OUT2" => InputSource.ARCHDMIOut2,
                    "@OK 5 OPTICAL" => InputSource.Optical,
                    "@OK 6 COAXIAL" => InputSource.Coaxial,
                    "@OK 7 USB-AUDIO" => InputSource.USBAudio,
                    _ => LogError(result.Response, InputSource.Unknown)
                }
            }
        };
    }

    public async ValueTask<OppoResult<InputSource>> SetInputSourceAsync(InputSource inputSource, CancellationToken cancellationToken = default)
    {
        if (!IsValidCommand(_model, inputSource))
            return false;
        
        ushort commandDigit = inputSource switch
        {
            InputSource.BluRayPlayer => 0,
            InputSource.HDMIIn => 1,
            InputSource.ARCHDMIOut => 2,
            InputSource.Optical => (ushort)(_is20XModel ? 3 : 5),
            InputSource.Coaxial => (ushort)(_is20XModel ? 4 : 6),
            InputSource.USBAudio => (ushort)(_is20XModel ? 5 : 7),
            InputSource.HDMIFront => 1,
            InputSource.HDMIBack => 2,
            InputSource.ARCHDMIOut1 => 3,
            InputSource.ARCHDMIOut2 => 4,
            InputSource.Unknown => throw new ArgumentOutOfRangeException(nameof(inputSource), inputSource, null),
            _ => throw new ArgumentOutOfRangeException(nameof(inputSource), inputSource, null)
        };
    
        var command = Encoding.ASCII.GetBytes(_is20XModel ? $"#SIS {commandDigit}\r" : $"REMOTE SIS {commandDigit}");
        var result = await SendCommandWithRetry(command, cancellationToken);
    
        return result.Success switch
        {
            false => false,
            _ => new OppoResult<InputSource>
            {
                Success = true,
                Result = result.Response switch
                {
                    "@OK 0 BD-PLAYER" => InputSource.BluRayPlayer,
                    "@OK 1 HDMI-IN" => InputSource.HDMIIn,
                    "@OK 2 ARC-HDMI-OUT" => InputSource.ARCHDMIOut,
                    "@OK 3 OPTICAL-IN" => InputSource.Optical,
                    "@OK 4 COAXIAL-IN" => InputSource.Coaxial,
                    "@OK 5 USB-AUDIO-IN" => InputSource.USBAudio,
                    "@OK 1 HDMI-FRONT" => InputSource.HDMIFront,
                    "@OK 2 HDMI-BACK" => InputSource.HDMIBack,
                    "@OK 3 ARC-HDMI-OUT1" => InputSource.ARCHDMIOut1,
                    "@OK 4 ARC-HDMI-OUT2" => InputSource.ARCHDMIOut2,
                    "@OK 5 OPTICAL" => InputSource.Optical,
                    "@OK 6 COAXIAL" => InputSource.Coaxial,
                    "@OK 7 USB-AUDIO" => InputSource.USBAudio,
                    _ => LogError(result.Response, InputSource.Unknown)
                }
            }
        };

        static bool IsValidCommand(in OppoModel model, in InputSource inputSource)
        {
            return model switch
            {
                OppoModel.BDP10X => inputSource switch
                {
                    InputSource.BluRayPlayer => true,
                    InputSource.HDMIFront => true,
                    InputSource.HDMIBack => true,
                    InputSource.ARCHDMIOut1 => true,
                    InputSource.ARCHDMIOut2 => true,
                    InputSource.Optical => true,
                    InputSource.Coaxial => true,
                    InputSource.USBAudio => true,
                    _ => false
                },
                OppoModel.UDP203 => inputSource switch
                {
                    InputSource.BluRayPlayer => true,
                    InputSource.HDMIIn => true,
                    InputSource.ARCHDMIOut => true,
                    _ => false
                },
                OppoModel.UDP205 => inputSource switch
                {
                    InputSource.BluRayPlayer => true,
                    InputSource.HDMIIn => true,
                    InputSource.Optical => true,
                    InputSource.USBAudio => true,
                    _ => false
                },
                _ => false
            };
        }
    }

    public async ValueTask<OppoResult<string>> QueryCDDBNumberAsync(CancellationToken cancellationToken = default)
    {
        if (!_is20XModel)
            return false;
        
        var result = await SendCommandWithRetry(Oppo20XQueryCommand.QueryCDDBNumber, cancellationToken);
        
        return result.Success switch
        {
            false => false,
            _ => new OppoResult<string>
            {
                Success = true,
                Result = result.Response[4..]
            }
        };
    }

    public async ValueTask<OppoResult<string>> QueryTrackNameAsync(CancellationToken cancellationToken = default)
    {
        if (!_is20XModel)
            return false;
        
        var result = await SendCommandWithRetry(Oppo20XQueryCommand.QueryTrackName, cancellationToken);
        
        return result.Success switch
        {
            false => false,
            _ => new OppoResult<string>
            {
                Success = true,
                Result = result.Response[4..]
            }
        };
    }

    public async ValueTask<OppoResult<string>> QueryTrackAlbumAsync(CancellationToken cancellationToken = default)
    {
        if (!_is20XModel)
            return false;
        
        var result = await SendCommandWithRetry(Oppo20XQueryCommand.QueryTrackAlbum, cancellationToken);
        
        return result.Success switch
        {
            false => false,
            _ => new OppoResult<string>
            {
                Success = true,
                Result = result.Response[4..]
            }
        };
    }

    public async ValueTask<OppoResult<string>> QueryTrackPerformerAsync(CancellationToken cancellationToken = default)
    {
        if (!_is20XModel)
            return false;
        
        var result = await SendCommandWithRetry(Oppo20XQueryCommand.QueryTrackPerformer, cancellationToken);
        
        return result.Success switch
        {
            false => false,
            _ => new OppoResult<string>
            {
                Success = true,
                Result = result.Response[4..]
            }
        };
    }

    public async ValueTask<OppoResult<VerboseMode>> SetVerboseMode(VerboseMode verboseMode, CancellationToken cancellationToken = default)
    {
        var command = verboseMode switch
        {
            VerboseMode.Off => _is20XModel ? Oppo20XAdvancedCommand.SetVerboseModeOff : Oppo10XAdvancedCommand.SetVerboseModeOff,
            VerboseMode.EchoCommandsInResponse => _is20XModel ? Oppo20XAdvancedCommand.SetVerboseEchoCommandsInResponse : Oppo10XAdvancedCommand.SetVerboseEchoCommandsInResponse,
            VerboseMode.ModeUnsolicitedStatusUpdates => _is20XModel ? Oppo20XAdvancedCommand.SetVerboseModeUnsolicitedStatusUpdates : Oppo10XAdvancedCommand.SetVerboseModeUnsolicitedStatusUpdates,
            VerboseMode.DetailedStatus => _is20XModel ? Oppo20XAdvancedCommand.SetVerboseModeDetailedStatus : Oppo10XAdvancedCommand.SetVerboseModeDetailedStatus,
            _ => throw new ArgumentOutOfRangeException(nameof(verboseMode), verboseMode, "Unknown verbose mode")
        };

        // Do not use SendCommandWithRetry here to avoid infinite loop
        var result = await SendCommand(command, cancellationToken);
        return result.Success switch
        {
            false => false,
            _ => new OppoResult<VerboseMode>
            {
                Success = true,
                Result = result.Response.AsSpan()[4..] switch
                {
                    "0" => VerboseMode.Off,
                    "1" => VerboseMode.EchoCommandsInResponse,
                    "2" => VerboseMode.ModeUnsolicitedStatusUpdates,
                    "3" => VerboseMode.DetailedStatus,
                    _ => LogError(result.Response, VerboseMode.Unknown)
                }
            }
        };
    }

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

        EnsureReaderLoopStarted();

        await foreach (var evt in channel.Reader.ReadAllAsync(cancellationToken))
            yield return evt;

        lock (_streamingSync)
        {
            if (ReferenceEquals(_streamingChannel, channel))
                _streamingChannel = null;
        }
    }

    public ValueTask<bool> IsConnectedAsync(TimeSpan? timeout = null)
        => ConnectHelper.IsConnectedAsync(_tcpClient, _hostName, _port, _semaphore, _logger, timeout);

    public string HostName => _hostName;
    
    private async ValueTask<OppoResultCore> SendCommand(byte[] command, CancellationToken cancellationToken, [CallerMemberName] string? caller = null)
    {
        if (!await _semaphore.WaitAsync(_timeout, cancellationToken))
            return OppoResultCore.FalseResult;

        try
        {
            if (_logger.IsEnabled(LogLevel.Trace))
                _logger.SendingCommand(Encoding.ASCII.GetString(command));

            if (Interlocked.CompareExchange(ref _failedResponseCount, 0, 2) > 2)
            {
                _logger.TooManyFailedResponses(caller);

                _tcpClient.Close();
                StopReaderLoop();
                _tcpClient = ConnectHelper.CreateTcpClient();
                await IsConnectedAsync();
            }

            EnsureReaderLoopStarted();

            var pendingResponse = new PendingCommandResponse(
                ExtractCommandCode(command),
                new TaskCompletionSource<OppoResultCore>(TaskCreationOptions.RunContinuationsAsynchronously));
            if (Interlocked.CompareExchange(ref _pendingCommandResponse, pendingResponse, null) is not null)
                return OppoResultCore.FalseResult;

            var networkStream = _tcpClient.GetStream();
            await networkStream.WriteAsync(command, cancellationToken);

            OppoResultCore result;
            try
            {
                result = await pendingResponse.Completion.Task.WaitAsync(TimeSpan.FromSeconds(1), cancellationToken);
            }
            catch (TimeoutException)
            {
                Interlocked.CompareExchange(ref _pendingCommandResponse, null, pendingResponse);
                _logger.CommandNotValidAtThisTime(caller);
                return OppoResultCore.FalseResult;
            }

            if (result.Success)
                return result;

            if (result.Response is { Length: > 0 } response)
            {
                _logger.FailedToSendCommand(caller, response);
                return response.StartsWith("@ER", StringComparison.Ordinal)
                    ? OppoResultCore.FalseResult
                    : OppoResultCore.InvalidVerboseLevelResult;
            }

            return OppoResultCore.FalseResult;
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

    private async ValueTask<OppoResultCore> SendCommandWithRetry(byte[] command, CancellationToken cancellationToken, [CallerMemberName] string? caller = null)
    {
        var result = await SendCommand(command, cancellationToken, caller);
        if (!result.InvalidVerboseLevel)
            return result;

        var verboseMode = await SetVerboseMode(VerboseMode.Off, cancellationToken);
        return !verboseMode.Success ? OppoResultCore.FalseResult : await SendCommand(command, cancellationToken, caller);
    }

    private void EnsureReaderLoopStarted()
    {
        if (_readerTask is { IsCompleted: false })
            return;

        lock (_streamingSync)
        {
            if (_readerTask is { IsCompleted: false })
                return;

            _readerCts = new CancellationTokenSource();
            var pipeReader = PipeReader.Create(_tcpClient.GetStream());
            _readerTask = Task.Run(() => ReaderLoopAsync(pipeReader, _readerCts.Token), _readerCts.Token);
        }
    }

    private async Task ReaderLoopAsync(PipeReader pipeReader, CancellationToken cancellationToken)
    {
        var completedByPeer = false;
        var canceled = false;
        var failed = false;

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var readResult = await pipeReader.ReadAsync(cancellationToken);
                var buffer = readResult.Buffer;

                while (true)
                {
                    var position = buffer.PositionOf((byte)0x0d);
                    if (position is null)
                        break;

                    var frameBuffer = buffer.Slice(0, position.Value);
                    HandleIncomingFrame(frameBuffer);

                    buffer = buffer.Slice(buffer.GetPosition(1, position.Value));
                }

                pipeReader.AdvanceTo(buffer.Start, buffer.End);

                if (readResult.IsCompleted)
                {
                    completedByPeer = true;
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            canceled = true;
        }
        catch (Exception e)
        {
            failed = true;
            _logger.FailedToSendCommandException(e);
        }
        finally
        {
            var pendingCommand = Interlocked.Exchange(ref _pendingCommandResponse, null);
            if (pendingCommand is not null)
            {
                if (completedByPeer)
                {
                    _logger.ReaderLoopCompletedWithPendingCommand();
                }
                else if (canceled)
                {
                    _logger.ReaderLoopCanceledWithPendingCommand();
                }
                else if (failed)
                {
                    _logger.ReaderLoopFailedWithPendingCommand();
                }

                pendingCommand.Completion.TrySetResult(OppoResultCore.FalseResult);
            }

            await pipeReader.CompleteAsync();
        }
    }

    private void HandleIncomingFrame(in ReadOnlySequence<byte> rawFrameBuffer)
    {
        if (TryGetCommandResponse(rawFrameBuffer, out var responseCommandCode, out var normalizedResponse)
            && TryCompletePendingCommand(responseCommandCode, normalizedResponse))
            return;

        if (!_logger.IsEnabled(LogLevel.Trace) && !HasStreamingSubscriber())
            return;

        if (TryParseStreamingEvent(rawFrameBuffer, out var evt))
        {
            if (evt is not null)
                PublishStreamingEvent(evt);
            return;
        }

        if (_logger.IsEnabled(LogLevel.Trace))
            _logger.ReceivedWireFrameRaw(Encoding.ASCII.GetString(rawFrameBuffer));
    }

    private void PublishStreamingEvent(OppoStreamingEvent evt)
    {
        Channel<OppoStreamingEvent>? channel;
        lock (_streamingSync)
            channel = _streamingChannel;

        if (channel is null)
        {
            if (_logger.IsEnabled(LogLevel.Trace))
                _logger.DroppedUnsubscribedStreamingFrame(Encoding.ASCII.GetString(evt.RawValue));
            return;
        }

        if (!channel.Writer.TryWrite(evt) && _logger.IsEnabled(LogLevel.Trace))
            _logger.DroppedUnsubscribedStreamingFrame(Encoding.ASCII.GetString(evt.RawValue));
    }

    private bool TryGetCommandResponse(in ReadOnlySequence<byte> rawFrameBuffer, out CommandCode? commandCode, out string normalizedResponse)
    {
        commandCode = null;
        normalizedResponse = string.Empty;

        if (rawFrameBuffer.Length is 0 or > int.MaxValue)
            return false;

        var frame = GetFrameSpan(rawFrameBuffer, out var rentedBuffer);
        try
        {
            // Non-streaming response
            if (frame.StartsWith("@OK"u8) || frame.StartsWith("@ER"u8))
            {
                normalizedResponse = Encoding.ASCII.GetString(frame);
                return true;
            }

            // Streaming reply format: @CMD OK ... / @CMD ER ...
            if (frame.Length > 6 && frame[0] == (byte)'@' && frame[4] == (byte)' ')
            {
                commandCode = CommandCode.TryParse(frame.Slice(1, 3), out var parsedCommandCode) ? parsedCommandCode : null;
                var payload = frame[5..];
                if (IsCommandResponsePayload(payload))
                {
                    normalizedResponse = string.Create(payload.Length + 1, payload, static (target, source) =>
                    {
                        target[0] = '@';
                        for (var i = 0; i < source.Length; i++)
                            target[i + 1] = (char)source[i];
                    });
                    return true;
                }
            }

            return false;
        }
        finally
        {
            if (rentedBuffer is not null)
                ArrayPool<byte>.Shared.Return(rentedBuffer);
        }

        static bool IsCommandResponsePayload(in ReadOnlySpan<byte> payload)
        {
            if (payload.Length < 2)
                return false;

            var first = payload[0];
            var second = payload[1];
            if (!((first == (byte)'O' && second == (byte)'K') || (first == (byte)'E' && second == (byte)'R')))
                return false;

            return payload.Length == 2 || payload[2] == (byte)' ';
        }
    }

    private bool HasStreamingSubscriber()
    {
        lock (_streamingSync)
            return _streamingChannel is not null;
    }

    private static ReadOnlySpan<byte> GetFrameSpan(in ReadOnlySequence<byte> rawFrameBuffer, out byte[]? rentedBuffer)
    {
        if (rawFrameBuffer.IsSingleSegment)
        {
            rentedBuffer = null;
            return rawFrameBuffer.FirstSpan;
        }

        var length = (int)rawFrameBuffer.Length;
        rentedBuffer = ArrayPool<byte>.Shared.Rent(length);
        rawFrameBuffer.CopyTo(rentedBuffer);
        return rentedBuffer.AsSpan(0, length);
    }

    private bool TryCompletePendingCommand(CommandCode? responseCommandCode, string normalizedResponse)
    {
        var pendingResponse = Volatile.Read(ref _pendingCommandResponse);
        if (pendingResponse is null)
            return false;

        if (responseCommandCode is not null
            && pendingResponse.Code is { } expectedCommandCode
            && responseCommandCode != expectedCommandCode)
        {
            return false;
        }

        if (!ReferenceEquals(Interlocked.CompareExchange(ref _pendingCommandResponse, null, pendingResponse), pendingResponse))
            return false;

        _logger.ReceivedResponse(normalizedResponse);
        var coreResult = normalizedResponse.StartsWith("@OK", StringComparison.Ordinal)
            ? OppoResultCore.SuccessResult(normalizedResponse)
            : new OppoResultCore(false, normalizedResponse);
        pendingResponse.Completion.TrySetResult(coreResult);
        return true;
    }

    private static CommandCode? ExtractCommandCode(ReadOnlySpan<byte> command)
    {
        return command.Length switch
        {
            >= 4 when command[0] == '#' => CommandCode.TryParse(command[1..4], out var hashCommandCode) ? hashCommandCode : null,
            >= 10 when command.StartsWith("REMOTE "u8) => CommandCode.TryParse(command.Slice(7, 3), out var remoteCommandCode) ? remoteCommandCode : null,
            _ => null
        };
    }

    private bool TryParseStreamingEvent(in ReadOnlySequence<byte> rawFrameBuffer, out OppoStreamingEvent? evt)
    {
        evt = null;

        if (rawFrameBuffer.Length is < 6 or > int.MaxValue)
            return false;

        var frame = GetFrameSpan(rawFrameBuffer, out var rentedBuffer);
        try
        {
            if (frame.Length < 6 || frame[0] != (byte)'@' || frame[4] != (byte)' ')
                return false;

            var code = frame.Slice(1, 3);
            var value = frame[5..];
            var frameBuffer = rawFrameBuffer;

            if (code.SequenceEqual("UPW"u8))
            {
                evt = new OppoPowerStateStreamingEvent(value switch
                    {
                        _ when value.SequenceEqual("1"u8) => PowerState.On,
                        _ when value.SequenceEqual("0"u8) => PowerState.Off,
                        _ => PowerState.Unknown
                    },
                    rawFrameBuffer);
                return true;
            }

            if (code.SequenceEqual("UPL"u8))
            {
                evt = new OppoPlaybackStatusStreamingEvent(
                    ParsePlaybackStatusUpdate(value),
                    rawFrameBuffer);
                return true;
            }

            if (code.SequenceEqual("UVL"u8))
            {
                var volumeInfo = value.SequenceEqual("MUT"u8)
                    ? new VolumeInfo(null, true)
                    : new VolumeInfo(TryParseUShort(value, out var parsedVolume) ? parsedVolume : null, false);
                evt = new OppoVolumeStreamingEvent(volumeInfo, rawFrameBuffer);
                return true;
            }

            if (code.SequenceEqual("UDT"u8))
            {
                evt = new OppoDiscTypeStreamingEvent(ParseDiscTypeUpdate(value), rawFrameBuffer);
                return true;
            }

            if (code.SequenceEqual("UAT"u8))
            {
                evt = new OppoAudioTypeStreamingEvent(Encoding.ASCII.GetString(value), rawFrameBuffer);
                return true;
            }

            if (code.SequenceEqual("UST"u8))
            {
                evt = new OppoSubtitleTypeStreamingEvent(Encoding.ASCII.GetString(value), rawFrameBuffer);
                return true;
            }

            if (code.SequenceEqual("UIS"u8))
            {
                evt = new OppoInputSourceStreamingEvent(
                    value switch
                    {
                        _ when value.SequenceEqual("0 BD-PLAYER"u8) => InputSource.BluRayPlayer,
                        _ when value.SequenceEqual("1 HDMI-IN"u8) => InputSource.HDMIIn,
                        _ when value.SequenceEqual("2 ARC-HDMI-OUT"u8) => InputSource.ARCHDMIOut,
                        _ when value.SequenceEqual("3 OPTICAL-IN"u8) => InputSource.Optical,
                        _ when value.SequenceEqual("4 COAXIAL-IN"u8) => InputSource.Coaxial,
                        _ when value.SequenceEqual("5 USB-AUDIO-IN"u8) => InputSource.USBAudio,
                        _ when value.SequenceEqual("1 HDMI-FRONT"u8) => InputSource.HDMIFront,
                        _ when value.SequenceEqual("2 HDMI-BACK"u8) => InputSource.HDMIBack,
                        _ when value.SequenceEqual("3 ARC-HDMI-OUT1"u8) => InputSource.ARCHDMIOut1,
                        _ when value.SequenceEqual("4 ARC-HDMI-OUT2"u8) => InputSource.ARCHDMIOut2,
                        _ when value.SequenceEqual("5 OPTICAL"u8) => InputSource.Optical,
                        _ when value.SequenceEqual("6 COAXIAL"u8) => InputSource.Coaxial,
                        _ when value.SequenceEqual("7 USB-AUDIO"u8) => InputSource.USBAudio,
                        _ => LogError(Encoding.ASCII.GetString(value), InputSource.Unknown)
                    },
                    rawFrameBuffer);
                return true;
            }

            if (code.SequenceEqual("U3D"u8))
            {
                evt = new OppoThreeDStatusStreamingEvent(value.SequenceEqual("3D"u8), rawFrameBuffer);
                return true;
            }

            if (code.SequenceEqual("UAR"u8))
            {
                evt = new OppoAspectRatioStreamingEvent(
                    value switch
                    {
                        _ when value.SequenceEqual("16WW"u8) => AspectRatio.A16WW,
                        _ when value.SequenceEqual("16AW"u8) => AspectRatio.A16AW,
                        _ when value.SequenceEqual("16A4"u8) => AspectRatio.A169A,
                        _ when value.SequenceEqual("21M0"u8) => AspectRatio.A21M0,
                        _ when value.SequenceEqual("21M1"u8) => AspectRatio.A21M1,
                        _ when value.SequenceEqual("21M2"u8) => AspectRatio.A21M2,
                        _ when value.SequenceEqual("21F0"u8) => AspectRatio.A21F0,
                        _ when value.SequenceEqual("21F1"u8) => AspectRatio.A21F1,
                        _ when value.SequenceEqual("21F2"u8) => AspectRatio.A21F2,
                        _ when value.SequenceEqual("21C0"u8) => AspectRatio.A21C0,
                        _ when value.SequenceEqual("21C1"u8) => AspectRatio.A21C1,
                        _ when value.SequenceEqual("21C2"u8) => AspectRatio.A21C2,
                        _ => LogError(Encoding.ASCII.GetString(value), AspectRatio.Unknown)
                    },
                    rawFrameBuffer);
                return true;
            }

            if (code.SequenceEqual("UTC"u8)
                && TryParseTimeCodeUpdate(value, out var title, out var chapter, out var timeCodeType, out var seconds))
            {
                evt = new OppoPlaybackProgressStreamingEvent(title, chapter, timeCodeType, seconds, rawFrameBuffer);
                return true;
            }

            if (code.SequenceEqual("UVO"u8))
            {
                evt = new OppoVideoResolutionStreamingEvent(ParseVideoResolutionUpdate(value), rawFrameBuffer);
                return true;
            }

            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.UnknownStreamingStatusCode(Encoding.ASCII.GetString(frameBuffer));
            evt = new OppoUnknownStreamingEvent(rawFrameBuffer);
            return true;
        }
        finally
        {
            if (rentedBuffer is not null)
                ArrayPool<byte>.Shared.Return(rentedBuffer);
        }
    }

    private static uint ParseTimeValue(ReadOnlySpan<byte> value)
    {
        var firstSeparator = value.IndexOf((byte)':');
        if (firstSeparator < 0)
            return 0;

        var remaining = value[(firstSeparator + 1)..];
        var secondSeparatorRelative = remaining.IndexOf((byte)':');
        if (secondSeparatorRelative < 0)
            return 0;

        var secondSeparator = firstSeparator + 1 + secondSeparatorRelative;

        return TryParseUInt32(value[..firstSeparator], out var hours)
               && TryParseUInt32(value[(firstSeparator + 1)..secondSeparator], out var minutes)
               && TryParseUInt32(value[(secondSeparator + 1)..], out var seconds)
            ? hours * 3600 + minutes * 60 + seconds
            : 0;
    }

    private bool TryParseTimeCodeUpdate(ReadOnlySpan<byte> value, out ushort title, out ushort chapter, out OppoTimeCodeType timeCodeType,
        out uint seconds)
    {
        title = 0;
        chapter = 0;
        timeCodeType = OppoTimeCodeType.Unknown;
        seconds = 0;

        var firstSpace = value.IndexOf((byte)' ');
        if (firstSpace <= 0)
            return false;

        var secondPart = value[(firstSpace + 1)..];
        var secondSpace = secondPart.IndexOf((byte)' ');
        if (secondSpace <= 0)
            return false;

        var thirdPart = secondPart[(secondSpace + 1)..];
        var thirdSpace = thirdPart.IndexOf((byte)' ');
        if (thirdSpace != 1)
            return false;

        var titleSpan = value[..firstSpace];
        var chapterSpan = secondPart[..secondSpace];
        var typeSpan = thirdPart[..1];
        var timeSpan = thirdPart[(thirdSpace + 1)..];

        if (!TryParseUShort(titleSpan, out title) || !TryParseUShort(chapterSpan, out chapter))
            return false;

        if (!TryParseTimeCodeType(typeSpan[0], out timeCodeType))
            return false;

        seconds = ParseTimeValue(timeSpan);
        return seconds > 0 || timeSpan.SequenceEqual("00:00:00"u8);
    }

    private bool TryParseTimeCodeType(byte value, out OppoTimeCodeType timeCodeType)
    {
        timeCodeType = value switch
        {
            (byte)'E' => OppoTimeCodeType.TotalElapsed,
            (byte)'R' => OppoTimeCodeType.TotalRemaining,
            (byte)'T' => OppoTimeCodeType.TitleElapsed,
            (byte)'X' => OppoTimeCodeType.TitleRemaining,
            (byte)'C' => OppoTimeCodeType.ChapterElapsed,
            (byte)'K' => OppoTimeCodeType.ChapterRemaining,
            _ => LogError(Encoding.ASCII.GetString([value]), OppoTimeCodeType.Unknown)
        };

        return timeCodeType != OppoTimeCodeType.Unknown;
    }

    private PlaybackStatus ParsePlaybackStatusUpdate(ReadOnlySpan<byte> value)
    {
        return value switch
        {
            _ when value.SequenceEqual("DISC"u8) => PlaybackStatus.NoDisc,
            _ when value.SequenceEqual("LOAD"u8) => PlaybackStatus.Loading,
            _ when value.SequenceEqual("OPEN"u8) => PlaybackStatus.Open,
            _ when value.SequenceEqual("CLOS"u8) => PlaybackStatus.Close,
            _ when value.SequenceEqual("PLAY"u8) => PlaybackStatus.Play,
            _ when value.SequenceEqual("PAUS"u8) => PlaybackStatus.Pause,
            _ when value.SequenceEqual("STOP"u8) => PlaybackStatus.Stop,
            _ when value.SequenceEqual("STPF"u8) || value.SequenceEqual("STPR"u8) => PlaybackStatus.Step,
            _ when value.Length == 4
                   && value.StartsWith("FFW"u8)
                   && value[3] is >= (byte)'1' and <= (byte)'5' => PlaybackStatus.FastForward,
            _ when value.Length == 4
                   && (value.StartsWith("FRV"u8) || value.StartsWith("FRE"u8))
                   && value[3] is >= (byte)'1' and <= (byte)'5' => PlaybackStatus.FastRewind,
            _ when value.Length == 4
                   && value.StartsWith("SFW"u8)
                   && value[3] is >= (byte)'1' and <= (byte)'5' => PlaybackStatus.SlowForward,
            _ when value.Length == 4
                   && (value.StartsWith("SRV"u8) || value.StartsWith("SRE"u8))
                   && value[3] is >= (byte)'1' and <= (byte)'5' => PlaybackStatus.SlowRewind,
            _ when value.SequenceEqual("HOME"u8) => PlaybackStatus.HomeMenu,
            _ when value.SequenceEqual("MCTR"u8) => PlaybackStatus.MediaCenter,
            _ when value.SequenceEqual("SCSV"u8) => PlaybackStatus.ScreenSaver,
            _ when value.SequenceEqual("MENU"u8) => PlaybackStatus.DiscMenu,
            _ => LogError(Encoding.ASCII.GetString(value), PlaybackStatus.Unknown)
        };
    }

    private DiscType ParseDiscTypeUpdate(ReadOnlySpan<byte> value)
    {
        return value switch
        {
            _ when value.SequenceEqual("UHBD"u8) => DiscType.UltraHDBluRay,
            _ when value.SequenceEqual("BDMV"u8) => DiscType.BlueRayMovie,
            _ when value.SequenceEqual("DVDV"u8) => DiscType.DVDVideo,
            _ when value.SequenceEqual("DVDA"u8) => DiscType.DVDAudio,
            _ when value.SequenceEqual("SACD"u8) => DiscType.SACD,
            _ when value.SequenceEqual("CDDA"u8) => DiscType.CDDiscAudio,
            _ when value.SequenceEqual("DATA"u8) => DiscType.DataDisc,
            _ when value.SequenceEqual("UNKW"u8) => DiscType.UnknownDisc,
            _ when value.SequenceEqual("HDCD"u8) => DiscType.HDCD,
            _ when value.SequenceEqual("VCD2"u8) => DiscType.VCD2,
            _ when value.SequenceEqual("SVCD"u8) => DiscType.SVCD,
            _ => LogError(Encoding.ASCII.GetString(value), DiscType.Unknown)
        };
    }

    private HDMIResolution ParseVideoResolutionUpdate(ReadOnlySpan<byte> value)
    {
        var inputPart = value.Contains((byte)' ') ? value[..value.IndexOf((byte)' ')] : value;
        return value switch
        {
            _ when inputPart.SequenceEqual("_480I60"u8) => HDMIResolution.R480i,
            _ when inputPart.SequenceEqual("_480P60"u8) => HDMIResolution.R480p,
            _ when inputPart.SequenceEqual("_576I50"u8) => HDMIResolution.R576i,
            _ when inputPart.SequenceEqual("_576P50"u8) => HDMIResolution.R576p,
            _ when inputPart.SequenceEqual("_720P50"u8) => HDMIResolution.R720p50,
            _ when inputPart.SequenceEqual("_720P60"u8) => HDMIResolution.R720p60,
            _ when inputPart.SequenceEqual("1080I50"u8) => HDMIResolution.R1080i50,
            _ when inputPart.SequenceEqual("1080I60"u8) => HDMIResolution.R1080i60,
            _ when inputPart.SequenceEqual("1080P23"u8) => HDMIResolution.R1080p23,
            _ when inputPart.SequenceEqual("1080P24"u8) => HDMIResolution.R1080p24,
            _ when inputPart.SequenceEqual("1080P50"u8) => HDMIResolution.R1080p50,
            _ when inputPart.SequenceEqual("1080P60"u8) => HDMIResolution.R1080p60,
            _ when inputPart.SequenceEqual("_UHD24_"u8) => HDMIResolution.RUltraHDp24,
            _ when inputPart.SequenceEqual("_UHD50_"u8) => HDMIResolution.RUltraHDp50,
            _ when inputPart.SequenceEqual("_UHD60_"u8) => HDMIResolution.RUltraHDp60,
            _ when inputPart.SequenceEqual("_OTHER_"u8) => HDMIResolution.Other,
            _ => LogError(Encoding.ASCII.GetString(value), HDMIResolution.Unknown)
        };
    }

    private static bool TryParseUShort(ReadOnlySpan<byte> value, out ushort result)
    {
        result = 0;
        if (value.IsEmpty)
            return false;

        foreach (var digit in value)
        {
            if (digit is < (byte)'0' or > (byte)'9')
                return false;

            if (result > ushort.MaxValue / 10)
                return false;

            var next = (result * 10) + (digit - (byte)'0');
            if (next > ushort.MaxValue)
                return false;

            result = (ushort)next;
        }

        return true;
    }

    private static bool TryParseUInt32(ReadOnlySpan<byte> value, out uint result)
    {
        result = 0;
        if (value.IsEmpty)
            return false;

        foreach (var digit in value)
        {
            if (digit is < (byte)'0' or > (byte)'9')
                return false;

            if (result > uint.MaxValue / 10)
                return false;

            var digitValue = (uint)(digit - (byte)'0');
            var multiplied = result * 10;
            if (multiplied > uint.MaxValue - digitValue)
                return false;

            result = multiplied + digitValue;
        }

        return true;
    }

    private void StopReaderLoop()
    {
        CancellationTokenSource? readerCts;
        Task? readerTask;

        lock (_streamingSync)
        {
            readerCts = _readerCts;
            readerTask = _readerTask;
            _readerCts = null;
            _readerTask = null;
        }

        if (readerCts is null)
            return;

        try
        {
            readerCts.Cancel();
        }
        catch (ObjectDisposedException)
        {
            return;
        }

        if (readerTask is null || readerTask.IsCompleted)
        {
            readerCts.Dispose();
            return;
        }

        _ = readerTask.ContinueWith(static (_, state) =>
            {
                ((CancellationTokenSource)state!).Dispose();
            },
            readerCts,
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }

    private uint _failedResponseCount;

    private TEnum LogError<TEnum>(string response, TEnum returnValue, [CallerMemberName]string? callerMemberName = null)
        where TEnum : Enum
    {
        Interlocked.Increment(ref _failedResponseCount);
        _logger.CallerMemberFailed(callerMemberName, response);
        return returnValue;
    }
    
    private async ValueTask<OppoResult<uint>> QueryTimeAsync(byte[] command, CancellationToken cancellationToken)
    {
        var result = await SendCommandWithRetry(command, cancellationToken);
        
        return result.Success switch
        {
            false => false,
            _ => new OppoResult<uint>
            {
                Success = true,
                Result = ParseTime(result.Response)
            }
        };
    }
    
    private static uint ParseTime(in ReadOnlySpan<char> response)
    {
        var time = response[4..];
        Span<Range> ranges = new Range[3];
        var parts = time.Split(ranges, ":", StringSplitOptions.TrimEntries);
        if (parts != 3)
            return 0;
        
        var hoursRange = ranges[0];
        var minutesRange = ranges[1];
        var secondsRange = ranges[2];
        return uint.Parse(time[hoursRange]) * 3600
               + uint.Parse(time[minutesRange]) * 60
               + uint.Parse(time[secondsRange]);
    }

    private readonly record struct CommandCode(uint Value)
    {
        public static bool TryParse(ReadOnlySpan<byte> value, out CommandCode commandCode)
        {
            if (value.Length == 3)
            {
                commandCode = new CommandCode(((uint)value[0] << 16) | ((uint)value[1] << 8) | value[2]);
                return true;
            }

            commandCode = default;
            return false;
        }
    }

    private sealed record PendingCommandResponse(CommandCode? Code, TaskCompletionSource<OppoResultCore> Completion);

    public void Dispose()
    {
        StopReaderLoop();

        lock (_streamingSync)
            _streamingChannel?.Writer.TryComplete();

        _tcpClient.Dispose();
        _semaphore.Dispose();
        IsDisposed = true;
    }
}
