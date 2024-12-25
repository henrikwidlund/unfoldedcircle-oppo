using System.Buffers;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;

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
        OppoModel.BDP8395 => 19999,
        OppoModel.BDP10X => 48360,
        OppoModel.UDP203 or OppoModel.UDP205 => 23,
        _ => throw new InvalidOperationException($"Model {model} is not supported.")
    };
    
    private readonly TcpClient _tcpClient = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly TimeSpan _timeout = TimeSpan.FromSeconds(1);
    private readonly StringBuilder _stringBuilder = new();
    
    private const string OkOn = "@OK ON";
    private const string OkOff = "@OK OFF";

    public async ValueTask<OppoResult<PowerState>> PowerToggleAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommand(
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
        var result = await SendCommand(
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
        var result = await SendCommand(
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
        var result = await SendCommand(
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
        var result = await SendCommand(
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
        var result = await SendCommand(
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
        var result = await SendCommand(
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
        var result = await SendCommand(
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
        var result = await SendCommand(
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
    
    public async ValueTask<bool> NumericInput([Range(0, 9)] ushort number, CancellationToken cancellationToken = default)
    {
        if (number > 9)
            return false;

        return (await SendCommand(
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
        (await SendCommand(_is20XModel ? Oppo20XCommand.Clear : Oppo10XCommand.Clear, cancellationToken)).Success;

    public async ValueTask<bool> GoToAsync(CancellationToken cancellationToken = default) =>
        (await SendCommand(_is20XModel ? Oppo20XCommand.GoTo : Oppo10XCommand.GoTo, cancellationToken)).Success;

    public async ValueTask<bool> HomeAsync(CancellationToken cancellationToken = default) =>
        (await SendCommand(_is20XModel ? Oppo20XCommand.Home : Oppo10XCommand.Home, cancellationToken)).Success;

    public async ValueTask<bool> PageUpAsync(CancellationToken cancellationToken = default) =>
        (await SendCommand(_is20XModel ? Oppo20XCommand.PageUp : Oppo10XCommand.PageUp, cancellationToken)).Success;

    public async ValueTask<bool> PageDownAsync(CancellationToken cancellationToken = default) =>
        (await SendCommand(_is20XModel ? Oppo20XCommand.PageDown : Oppo10XCommand.PageDown, cancellationToken)).Success;

    public async ValueTask<bool> InfoToggleAsync(CancellationToken cancellationToken = default) =>
        (await SendCommand(_is20XModel ? Oppo20XCommand.InfoToggle : Oppo10XCommand.InfoToggle, cancellationToken)).Success;

    public async ValueTask<bool> TopMenuAsync(CancellationToken cancellationToken = default) =>
        (await SendCommand(_is20XModel ? Oppo20XCommand.TopMenu : Oppo10XCommand.TopMenu, cancellationToken)).Success;

    public async ValueTask<bool> PopUpMenuAsync(CancellationToken cancellationToken = default) =>
        (await SendCommand(_is20XModel ? Oppo20XCommand.PopUpMenu : Oppo10XCommand.PopUpMenu, cancellationToken)).Success;

    public async ValueTask<bool> UpArrowAsync(CancellationToken cancellationToken = default) =>
        (await SendCommand(_is20XModel ? Oppo20XCommand.UpArrow : Oppo10XCommand.UpArrow, cancellationToken)).Success;

    public async ValueTask<bool> LeftArrowAsync(CancellationToken cancellationToken = default) =>
        (await SendCommand(_is20XModel ? Oppo20XCommand.LeftArrow : Oppo10XCommand.LeftArrow, cancellationToken)).Success;

    public async ValueTask<bool> RightArrowAsync(CancellationToken cancellationToken = default) =>
        (await SendCommand(_is20XModel ? Oppo20XCommand.RightArrow : Oppo10XCommand.RightArrow, cancellationToken)).Success;

    public async ValueTask<bool> DownArrowAsync(CancellationToken cancellationToken = default) =>
        (await SendCommand(_is20XModel ? Oppo20XCommand.DownArrow : Oppo10XCommand.DownArrow, cancellationToken)).Success;

    public async ValueTask<bool> EnterAsync(CancellationToken cancellationToken = default) =>
        (await SendCommand(_is20XModel ? Oppo20XCommand.Enter : Oppo10XCommand.Enter, cancellationToken)).Success;

    public async ValueTask<bool> SetupAsync(CancellationToken cancellationToken = default) =>
        (await SendCommand(_is20XModel ? Oppo20XCommand.Setup : Oppo10XCommand.Setup, cancellationToken)).Success;

    public async ValueTask<bool> ReturnAsync(CancellationToken cancellationToken = default) =>
        (await SendCommand(_is20XModel ? Oppo20XCommand.Return : Oppo10XCommand.Return, cancellationToken)).Success;

    public async ValueTask<bool> RedAsync(CancellationToken cancellationToken = default) =>
        (await SendCommand(_is20XModel ? Oppo20XCommand.Red : Oppo10XCommand.Red, cancellationToken)).Success;

    public async ValueTask<bool> GreenAsync(CancellationToken cancellationToken = default) =>
        (await SendCommand(_is20XModel ? Oppo20XCommand.Green : Oppo10XCommand.Green, cancellationToken)).Success;

    public async ValueTask<bool> BlueAsync(CancellationToken cancellationToken = default) =>
        (await SendCommand(_is20XModel ? Oppo20XCommand.Blue : Oppo10XCommand.Blue, cancellationToken)).Success;

    public async ValueTask<bool> YellowAsync(CancellationToken cancellationToken = default) =>
        (await SendCommand(_is20XModel ? Oppo20XCommand.Yellow : Oppo10XCommand.Yellow, cancellationToken)).Success;

    public async ValueTask<bool> StopAsync(CancellationToken cancellationToken = default) =>
        (await SendCommand(_is20XModel ? Oppo20XCommand.Stop : Oppo10XCommand.Stop, cancellationToken)).Success;

    public async ValueTask<bool> PlayAsync(CancellationToken cancellationToken = default) =>
        (await SendCommand(_is20XModel ? Oppo20XCommand.Play : Oppo10XCommand.Play, cancellationToken)).Success;

    public async ValueTask<bool> PauseAsync(CancellationToken cancellationToken = default) =>
        (await SendCommand(_is20XModel ? Oppo20XCommand.Pause : Oppo10XCommand.Pause, cancellationToken)).Success;

    public async ValueTask<bool> PreviousAsync(CancellationToken cancellationToken = default) =>
        (await SendCommand(_is20XModel ? Oppo20XCommand.Previous : Oppo10XCommand.Previous, cancellationToken)).Success;

    public async ValueTask<OppoResult<ushort?>> ReverseAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommand(
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
        var result = await SendCommand(
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
        (await SendCommand(_is20XModel ? Oppo20XCommand.Next : Oppo10XCommand.Next, cancellationToken)).Success;

    public async ValueTask<bool> AudioAsync(CancellationToken cancellationToken = default) =>
        (await SendCommand(_is20XModel ? Oppo20XCommand.Audio : Oppo10XCommand.Audio, cancellationToken)).Success;

    public async ValueTask<bool> SubtitleAsync(CancellationToken cancellationToken = default) =>
        (await SendCommand(_is20XModel ? Oppo20XCommand.Subtitle : Oppo10XCommand.Subtitle, cancellationToken)).Success;

    public async ValueTask<OppoResult<string>> AngleAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommand(
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
        var result = await SendCommand(
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
        var result = await SendCommand(
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
        var result = await SendCommand(
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
        var result = await SendCommand(
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
        var result = await SendCommand(
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
        (await SendCommand(_is20XModel ? Oppo20XCommand.Resolution : Oppo10XCommand.Resolution, cancellationToken)).Success;

    public async ValueTask<bool> SubtitleHoldAsync(CancellationToken cancellationToken = default) =>
        (await SendCommand(_is20XModel ? Oppo20XCommand.SubtitleHold : Oppo10XCommand.SubtitleHold, cancellationToken)).Success;

    public async ValueTask<bool> OptionAsync(CancellationToken cancellationToken = default) =>
        _model is not OppoModel.BDP8395 &&
        (await SendCommand(_is20XModel ? Oppo20XCommand.Option : Oppo10XCommand.Option, cancellationToken)).Success;

    public async ValueTask<bool> ThreeDAsync(CancellationToken cancellationToken = default) =>
        _model is not OppoModel.BDP8395 &&
        (await SendCommand(_is20XModel ? Oppo20XCommand.ThreeD : Oppo10XCommand.ThreeD, cancellationToken)).Success;

    public async ValueTask<bool> PictureAdjustmentAsync(CancellationToken cancellationToken = default) =>
        _model is not OppoModel.BDP8395 &&
        (await SendCommand(_is20XModel ? Oppo20XCommand.PictureAdjustment : Oppo10XCommand.PictureAdjustment, cancellationToken)).Success;

    public async ValueTask<bool> HDRAsync(CancellationToken cancellationToken = default) =>
        _is20XModel &&
        (await SendCommand(Oppo20XCommand.HDR, cancellationToken)).Success;

    public async ValueTask<bool> InfoHoldAsync(CancellationToken cancellationToken = default) =>
        _is20XModel &&
        (await SendCommand(Oppo20XCommand.InfoHold, cancellationToken)).Success;

    public async ValueTask<bool> ResolutionHoldAsync(CancellationToken cancellationToken = default) =>
        _is20XModel &&
        (await SendCommand(Oppo20XCommand.ResolutionHold, cancellationToken)).Success;

    public async ValueTask<bool> AVSyncAsync(CancellationToken cancellationToken = default) =>
        _is20XModel &&
        (await SendCommand(Oppo20XCommand.AVSync, cancellationToken)).Success;

    public async ValueTask<bool> GaplessPlayAsync(CancellationToken cancellationToken = default) =>
        _is20XModel && (await SendCommand(Oppo20XCommand.GaplessPlay, cancellationToken)).Success;

    public async ValueTask<bool> NoopAsync(CancellationToken cancellationToken = default) =>
        (await SendCommand(_is20XModel ? Oppo20XCommand.Noop : Oppo10XCommand.Noop, cancellationToken)).Success;

    public async ValueTask<bool> InputAsync(CancellationToken cancellationToken = default) =>
        (await SendCommand(_is20XModel ? Oppo20XCommand.Input : Oppo10XCommand.Input, cancellationToken)).Success;

    public async ValueTask<OppoResult<RepeatMode>> SetRepeatAsync(RepeatMode mode, CancellationToken cancellationToken = default)
    {
        if (mode == RepeatMode.Unknown)
            return false;
        
        var result = await SendCommand(mode switch
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
            false => new OppoResult<RepeatMode> { Success = false },
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
        var result = await SendCommand(command, cancellationToken);
        
        return result.Success switch
        {
            false => new OppoResult<ushort> { Success = false },
            _ => new OppoResult<ushort>
            {
                Success = ushort.TryParse(result.Response.AsSpan()[4..], out var newVolume),
                Result = newVolume
            }
        };
    }

    public async ValueTask<OppoResult<VolumeInfo>> QueryVolumeAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendCommand(_is20XModel ? Oppo20XQueryCommand.QueryVolume : Oppo10XQueryCommand.QueryVolume, cancellationToken);

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
        var result = await SendCommand(command, cancellationToken);
        
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
        var result = await SendCommand(command, cancellationToken);
        
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
                    "OK CLOSE" => PlaybackStatus.Close,
                    "@OK UNKNOW" => PlaybackStatus.Unknown,
                    _ => LogError(result.Response, PlaybackStatus.Unknown)
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
        var result = await SendCommand(command, cancellationToken);
        
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
                    _ => LogError(result.Response, DiscType.UnknownDisc)
                }
            }
        };
    }

    public async ValueTask<OppoResult<CurrentRepeatMode>> QueryRepeatModeAsync(CancellationToken cancellationToken = default)
    {
        var command = _is20XModel ? Oppo20XQueryCommand.QueryRepeatMode : Oppo10XQueryCommand.QueryRepeatMode;
        var result = await SendCommand(command, cancellationToken);
        
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
        if (_model is OppoModel.BDP8395)
            return false;
        
        var result = await SendCommand(_is20XModel ? Oppo20XQueryCommand.QueryInputSource : Oppo10XQueryCommand.QueryInputSource,
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
    
        var command = Encoding.ASCII.GetBytes(_is20XModel ? $"#SIS {commandDigit}\r" : $"REMOTE SVL {commandDigit}");
        var result = await SendCommand(command, cancellationToken);
    
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
        
        var result = await SendCommand(Oppo20XQueryCommand.QueryCDDBNumber, cancellationToken);
        
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
        
        var result = await SendCommand(Oppo20XQueryCommand.QueryTrackName, cancellationToken);
        
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
        
        var result = await SendCommand(Oppo20XQueryCommand.QueryTrackAlbum, cancellationToken);
        
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
        
        var result = await SendCommand(Oppo20XQueryCommand.QueryTrackPerformer, cancellationToken);
        
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

    public async ValueTask<bool> IsConnectedAsync(TimeSpan? timeout = null)
    {
        // check twice, once before the wait, and once after the wait
        if (!_tcpClient.Connected && (await _semaphore.WaitAsync(timeout ?? TimeSpan.FromSeconds(9)) && !_tcpClient.Connected))
        {
            try
            {
                using var cancellationTokenSource = new CancellationTokenSource(timeout ?? TimeSpan.FromSeconds(10));
                await _tcpClient.ConnectAsync(_hostName, _port, cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                // nothing to do here, ignore
            }
            finally
            {
                _semaphore.Release();
            }
        }
        
        return _tcpClient.Connected;
    }

    public string GetHost() => _hostName;
    
    private async ValueTask<OppoResultCore> SendCommand(byte[] command, CancellationToken cancellationToken, [CallerMemberName] string? caller = null)
    {
        if (!await _semaphore.WaitAsync(_timeout, cancellationToken))
            return OppoResultCore.FalseResult;

        try
        {
            if (_logger.IsEnabled(LogLevel.Trace))
                _logger.LogTrace("Sending command '{Command}'", Encoding.ASCII.GetString(command));

            var networkStream = _tcpClient.GetStream();
            await networkStream.WriteAsync(command, cancellationToken);

            var response = await ReadUntilCarriageReturnAsync(networkStream, cancellationToken);

            if (_logger.IsEnabled(LogLevel.Trace))
                _logger.LogTrace("Received response '{Response}'", response);

            if (response is { Length: >= 3 } && response.AsSpan()[..3] is "@OK")
                return OppoResultCore.SuccessResult(response);

            if (response is { Length: 0 })
            {
                _logger.LogDebug("{Caller} - Command not valid at this time", caller);
                return OppoResultCore.FalseResult;
            }

            _logger.LogError("{Caller} - Failed to send command. Response was '{Response}'", caller, response);

            return OppoResultCore.FalseResult;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to send command");
            return OppoResultCore.FalseResult;
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    private async ValueTask<string> ReadUntilCarriageReturnAsync(NetworkStream networkStream,
        CancellationToken cancellationToken = default)
    {
        _stringBuilder.Clear();
        var pipeReader = PipeReader.Create(networkStream);
        var charBuffer = ArrayPool<char>.Shared.Rent(1024);
        var firstWrite = true;
        
        try
        {
            while (true)
            {
                var result = await pipeReader.ReadAsync(cancellationToken);
                var buffer = result.Buffer;
                SequencePosition? position;

                do
                {
                    position = buffer.PositionOf((byte)0x0d); // ASCII 0x0d (carriage return)

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
                } while (position == null && !result.IsCompleted);

                if (result.IsCompleted)
                {
                    break;
                }
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
            if (!_is20XModel && (!span.StartsWith("@OK "u8) || !span.StartsWith("@ER "u8)))
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

    private readonly record struct OppoResultCore(
        bool Success,
        string? Response)
    {
        public string? Response { get; } = Response;

        [MemberNotNullWhen(true, nameof(Response))]
        public bool Success { get; } = Success;
        
        public static readonly OppoResultCore FalseResult = new(false, null);
        
        public static OppoResultCore SuccessResult(string response) => new(true, response);
    }
    
    private TEnum LogError<TEnum>(string response, TEnum returnValue, [CallerMemberName]string? callerMemberName = null)
        where TEnum : Enum
    {
        _logger.LogError("{CallerMemberName} failed. Response was {Response}", callerMemberName, response);
        return returnValue;
    }
    
    private async ValueTask<OppoResult<uint>> QueryTimeAsync(byte[] command, CancellationToken cancellationToken)
    {
        var result = await SendCommand(command, cancellationToken);
        
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

    public void Dispose()
    {
        _tcpClient.Dispose();
        _semaphore.Dispose();
    }
}