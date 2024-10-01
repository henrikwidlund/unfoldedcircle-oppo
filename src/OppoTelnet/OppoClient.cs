using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.Logging;
using PrimS.Telnet;

using static OppoTelnet.Command;
using static OppoTelnet.AdvancedCommand;
using static OppoTelnet.QueryCommand;

namespace OppoTelnet;

public sealed class OppoClient : IOppoClient
{
    private readonly TcpByteStream _byteStream;
    private readonly Client _client;
    private readonly string _hostName;
    private readonly ILogger<OppoClient> _logger;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly TimeSpan _timeout = TimeSpan.FromSeconds(1);

    public OppoClient(string hostName, in OppoModel model, ILogger<OppoClient> logger)
    {
        _byteStream = new TcpByteStream(hostName, (ushort)model);
        _client = new Client(_byteStream, TimeSpan.FromSeconds(10), CancellationToken.None);
        _hostName = hostName;
        _logger = logger;
    }

    public async ValueTask<OppoResult<PowerState>> PowerToggleAsync(CancellationToken cancellationToken = default)
    {
        if (!await _semaphore.WaitAsync(_timeout, cancellationToken))
            return false;
        
        try
        {
            var result = await SendCommand(PowerToggle);
            return result.Success switch
            {
                false => new OppoResult<PowerState> { Success = false },
                _ => new OppoResult<PowerState>
                {
                    Success = true,
                    Result = result.Response switch
                    {
                        "@OK ON\r" => PowerState.On,
                        "@OK OFF\r" => PowerState.Off,
                        _ => LogError(result.Response, PowerState.Unknown)
                    }
                }
            };
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async ValueTask<OppoResult<PowerState>> PowerOnAsync(CancellationToken cancellationToken = default)
    {
        if (!await _semaphore.WaitAsync(_timeout, cancellationToken))
            return false;

        try
        {
            var result = await SendCommand(PowerOn);

            return result.Success switch
            {
                false => new OppoResult<PowerState> { Success = false },
                _ => new OppoResult<PowerState>
                {
                    Success = true,
                    Result = result.Response switch
                    {
                        "@OK ON\r" => PowerState.On,
                        _ => LogError(result.Response, PowerState.Unknown)
                    }
                }
            };
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async ValueTask<OppoResult<PowerState>> PowerOffAsync(CancellationToken cancellationToken = default)
    {
        if (!await _semaphore.WaitAsync(_timeout, cancellationToken))
            return false;
        
        try
        {
            var result = await SendCommand(PowerOff);

            return result.Success switch
            {
                false => new OppoResult<PowerState> { Success = false },
                _ => new OppoResult<PowerState>
                {
                    Success = true,
                    Result = result.Response switch
                    {
                        "@OK OFF\r" => PowerState.Off,
                        _ => LogError(result.Response, PowerState.Unknown)
                    }
                }
            };
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async ValueTask<OppoResult<TrayState>> EjectToggleAsync(CancellationToken cancellationToken = default)
    {
        if (!await _semaphore.WaitAsync(_timeout, cancellationToken))
            return false;
        
        try
        {
            var result = await SendCommand(EjectToggle);

            return result.Success switch
            {
                false => new OppoResult<TrayState> { Success = false },
                _ => new OppoResult<TrayState>
                {
                    Success = true,
                    Result = result.Response switch
                    {
                        "@OK OPEN\r" => TrayState.Open,
                        "@OK CLOSE\r" => TrayState.Closed,
                        _ => LogError(result.Response, TrayState.Unknown)
                    }
                }
            };
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async ValueTask<OppoResult<DimmerState>> DimmerAsync(CancellationToken cancellationToken = default)
    {
        if (!await _semaphore.WaitAsync(_timeout, cancellationToken))
            return false;

        try
        {
            var result = await SendCommand(Dimmer);

            return result.Success switch
            {
                false => new OppoResult<DimmerState> { Success = false },
                _ => new OppoResult<DimmerState>
                {
                    Success = true,
                    Result = result.Response switch
                    {
                        "@OK ON\r" => DimmerState.On,
                        "@OK DIM\r" => DimmerState.Dim,
                        "@OK OFF\r" => DimmerState.Off,
                        _ => LogError(result.Response, DimmerState.Unknown)
                    }
                }
            };
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async ValueTask<OppoResult<PureAudioState>> PureAudioToggleAsync(CancellationToken cancellationToken = default)
    {
        if (!await _semaphore.WaitAsync(_timeout, cancellationToken))
            return false;
        
        try
        {
            var result = await SendCommand(PureAudioToggle);

            return result.Success switch
            {
                false => new OppoResult<PureAudioState> { Success = false },
                _ => new OppoResult<PureAudioState>
                {
                    Success = true,
                    Result = result.Response switch
                    {
                        "@OK ON\r" => PureAudioState.On,
                        "@OK OFF\r" => PureAudioState.Off,
                        _ => LogError(result.Response, PureAudioState.Unknown)
                    }
                }
            };
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async ValueTask<OppoResult<ushort?>> VolumeUpAsync(CancellationToken cancellationToken = default)
    {
        if (!await _semaphore.WaitAsync(_timeout, cancellationToken))
            return false;
        
        try
        {
            var result = await SendCommand(VolumeUp);

            return result.Success switch
            {
                false => new OppoResult<ushort?> { Success = false },
                _ => new OppoResult<ushort?>
                {
                    Success = ushort.TryParse(result.Response.AsSpan()[4..^1], out var volume),
                    Result = volume
                }
            };
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    public async ValueTask<OppoResult<ushort?>> VolumeDownAsync(CancellationToken cancellationToken = default)
    {
        if (!await _semaphore.WaitAsync(_timeout, cancellationToken))
            return false;
        
        try
        {
            var result = await SendCommand(VolumeDown);

            return result.Success switch
            {
                false => new OppoResult<ushort?> { Success = false },
                _ => new OppoResult<ushort?>
                {
                    Success = ushort.TryParse(result.Response.AsSpan()[4..^1], out var volume),
                    Result = volume
                }
            };
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    public async ValueTask<OppoResult<MuteState>> MuteToggleAsync(CancellationToken cancellationToken = default)
    {
        if (!await _semaphore.WaitAsync(_timeout, cancellationToken))
            return false;
        
        try
        {
            var result = await SendCommand(MuteToggle);

            return result.Success switch
            {
                false => new OppoResult<MuteState> { Success = false },
                _ => new OppoResult<MuteState>
                {
                    Success = true,
                    Result = result.Response switch
                    {
                        "@OK MUTE\r" => MuteState.On,
                        "@OK UNMUTE\r" => MuteState.Off,
                        _ => LogError(result.Response, MuteState.Unknown)
                    }
                }
            };
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    public async ValueTask<bool> NumericInput([Range(0, 9)] ushort number, CancellationToken cancellationToken = default)
    {
        if (number > 9)
            return false;
        
        if (!await _semaphore.WaitAsync(_timeout, cancellationToken))
            return false;
        
        try
        {
            var result = await SendCommand(number switch
            {
                0 => NumericKey0,
                1 => NumericKey1,
                2 => NumericKey2,
                3 => NumericKey3,
                4 => NumericKey4,
                5 => NumericKey5,
                6 => NumericKey6,
                7 => NumericKey7,
                8 => NumericKey8,
                9 => NumericKey9,
                _ => throw new ArgumentOutOfRangeException(nameof(number))
            });
            return result.Success;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async ValueTask<bool> ClearAsync(CancellationToken cancellationToken = default)
    {
        if (!await _semaphore.WaitAsync(_timeout, cancellationToken))
            return false;
        
        try
        {
            var result = await SendCommand(Clear);
            return result.Success;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async ValueTask<bool> GoToAsync(CancellationToken cancellationToken = default)
    {
        if (!await _semaphore.WaitAsync(_timeout, cancellationToken))
            return false;
        
        try
        {
            var result = await SendCommand(GoTo);
            return result.Success;
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    public async ValueTask<bool> HomeAsync(CancellationToken cancellationToken = default)
    {
        if (!await _semaphore.WaitAsync(_timeout, cancellationToken))
            return false;
        
        try
        {
            var result = await SendCommand(Home);
            return result.Success;
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    public async ValueTask<bool> PageUpAsync(CancellationToken cancellationToken = default)
    {
        if (!await _semaphore.WaitAsync(_timeout, cancellationToken))
            return false;
        
        try
        {
            var result = await SendCommand(PageUp);
            return result.Success;
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    public async ValueTask<bool> PageDownAsync(CancellationToken cancellationToken = default)
    {
        if (!await _semaphore.WaitAsync(_timeout, cancellationToken))
            return false;
        
        try
        {
            var result = await SendCommand(PageDown);
            return result.Success;
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    public async ValueTask<bool> InfoToggleAsync(CancellationToken cancellationToken = default)
    {
        if (!await _semaphore.WaitAsync(_timeout, cancellationToken))
            return false;
        
        try
        {
            var result = await SendCommand(InfoToggle);
            return result.Success;
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    public async ValueTask<bool> TopMenuAsync(CancellationToken cancellationToken = default)
    {
        if (!await _semaphore.WaitAsync(_timeout, cancellationToken))
            return false;
        
        try
        {
            var result = await SendCommand(TopMenu);
            return result.Success;
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    public async ValueTask<bool> PopUpMenuAsync(CancellationToken cancellationToken = default)
    {
        if (!await _semaphore.WaitAsync(_timeout, cancellationToken))
            return false;
        
        try
        {
            var result = await SendCommand(PopUpMenu);
            return result.Success;
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    public async ValueTask<bool> UpArrowAsync(CancellationToken cancellationToken = default)
    {
        if (!await _semaphore.WaitAsync(_timeout, cancellationToken))
            return false;
        
        try
        {
            var result = await SendCommand(UpArrow);
            return result.Success;
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    public async ValueTask<bool> LeftArrowAsync(CancellationToken cancellationToken = default)
    {
        if (!await _semaphore.WaitAsync(_timeout, cancellationToken))
            return false;
        
        try
        {
            var result = await SendCommand(LeftArrow);
            return result.Success;
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    public async ValueTask<bool> RightArrowAsync(CancellationToken cancellationToken = default)
    {
        if (!await _semaphore.WaitAsync(_timeout, cancellationToken))
            return false;
        
        try
        {
            var result = await SendCommand(RightArrow);
            return result.Success;
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    public async ValueTask<bool> DownArrowAsync(CancellationToken cancellationToken = default)
    {
        if (!await _semaphore.WaitAsync(_timeout, cancellationToken))
            return false;
        
        try
        {
            var result = await SendCommand(DownArrow);
            return result.Success;
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    public async ValueTask<bool> EnterAsync(CancellationToken cancellationToken = default)
    {
        if (!await _semaphore.WaitAsync(_timeout, cancellationToken))
            return false;
        
        try
        {
            var result = await SendCommand(Enter);
            return result.Success;
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    public async ValueTask<bool> SetupAsync(CancellationToken cancellationToken = default)
    {
        if (!await _semaphore.WaitAsync(_timeout, cancellationToken))
            return false;
        
        try
        {
            var result = await SendCommand(Setup);
            return result.Success;
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    public async ValueTask<bool> ReturnAsync(CancellationToken cancellationToken = default)
    {
        if (!await _semaphore.WaitAsync(_timeout, cancellationToken))
            return false;
        
        try
        {
            var result = await SendCommand(Return);
            return result.Success;
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    public async ValueTask<bool> RedAsync(CancellationToken cancellationToken = default)
    {
        if (!await _semaphore.WaitAsync(_timeout, cancellationToken))
            return false;
        
        try
        {
            var result = await SendCommand(Red);
            return result.Success;
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    public async ValueTask<bool> GreenAsync(CancellationToken cancellationToken = default)
    {
        if (!await _semaphore.WaitAsync(_timeout, cancellationToken))
            return false;
        
        try
        {
            var result = await SendCommand(Green);
            return result.Success;
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    public async ValueTask<bool> BlueAsync(CancellationToken cancellationToken = default)
    {
        if (!await _semaphore.WaitAsync(_timeout, cancellationToken))
            return false;
        
        try
        {
            var result = await SendCommand(Blue);
            return result.Success;
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    public async ValueTask<bool> YellowAsync(CancellationToken cancellationToken = default)
    {
        if (!await _semaphore.WaitAsync(_timeout, cancellationToken))
            return false;
        
        try
        {
            var result = await SendCommand(Yellow);
            return result.Success;
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    public async ValueTask<bool> StopAsync(CancellationToken cancellationToken = default)
    {
        if (!await _semaphore.WaitAsync(_timeout, cancellationToken))
            return false;
        
        try
        {
            var result = await SendCommand(Stop);
            return result.Success;
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    public async ValueTask<bool> PlayAsync(CancellationToken cancellationToken = default)
    {
        if (!await _semaphore.WaitAsync(_timeout, cancellationToken))
            return false;
        
        try
        {
            var result = await SendCommand(Play);
            return result.Success;
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    public async ValueTask<bool> PauseAsync(CancellationToken cancellationToken = default)
    {
        if (!await _semaphore.WaitAsync(_timeout, cancellationToken))
            return false;
        
        try
        {
            var result = await SendCommand(Pause);
            return result.Success;
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    public async ValueTask<bool> PreviousAsync(CancellationToken cancellationToken = default)
    {
        if (!await _semaphore.WaitAsync(_timeout, cancellationToken))
            return false;
        
        try
        {
            var result = await SendCommand(Previous);
            return result.Success;
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    public async ValueTask<OppoResult<ushort?>> ReverseAsync(CancellationToken cancellationToken = default)
    {
        if (!await _semaphore.WaitAsync(_timeout, cancellationToken))
            return false;
        
        try
        {
            var result = await SendCommand(Reverse);
        
            return result.Success switch
            {
                false => new OppoResult<ushort?> { Success = false },
                _ => new OppoResult<ushort?>
                {
                    Success = ushort.TryParse(result.Response.AsSpan()[4..^1], out var speed),
                    Result = speed
                }
            };
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    public async ValueTask<OppoResult<ushort?>> ForwardAsync(CancellationToken cancellationToken = default)
    {
        if (!await _semaphore.WaitAsync(_timeout, cancellationToken))
            return false;
        
        try
        {
            var result = await SendCommand(Forward);
        
            return result.Success switch
            {
                false => new OppoResult<ushort?> { Success = false },
                _ => new OppoResult<ushort?>
                {
                    Success = ushort.TryParse(result.Response.AsSpan()[4..^1], out var speed),
                    Result = speed
                }
            };
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    public async ValueTask<bool> NextAsync(CancellationToken cancellationToken = default)
    {
        if (!await _semaphore.WaitAsync(_timeout, cancellationToken))
            return false;
        
        try
        {
            var result = await SendCommand(Next);
            return result.Success;
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    public async ValueTask<bool> AudioAsync(CancellationToken cancellationToken = default)
    {
        if (!await _semaphore.WaitAsync(_timeout, cancellationToken))
            return false;
        
        try
        {
            var result = await SendCommand(Audio);
            return result.Success;
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    public async ValueTask<bool> SubtitleAsync(CancellationToken cancellationToken = default)
    {
        if (!await _semaphore.WaitAsync(_timeout, cancellationToken))
            return false;
        
        try
        {
            var result = await SendCommand(Subtitle);
            return result.Success;
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    public async ValueTask<OppoResult<string>> AngleAsync(CancellationToken cancellationToken = default)
    {
        if (!await _semaphore.WaitAsync(_timeout, cancellationToken))
            return false;
        
        try
        {
            var result = await SendCommand(Angle);
        
            return result.Success switch
            {
                false => new OppoResult<string> { Success = false },
                _ => new OppoResult<string>
                {
                    Success = true,
                    Result = result.Response[4..]
                }
            };
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    public async ValueTask<OppoResult<string>> ZoomAsync(CancellationToken cancellationToken = default)
    {
        if (!await _semaphore.WaitAsync(_timeout, cancellationToken))
            return false;
        
        try
        {
            var result = await SendCommand(Zoom);
        
            return result.Success switch
            {
                false => new OppoResult<string> { Success = false },
                _ => new OppoResult<string>
                {
                    Success = true,
                    Result = result.Response[4..]
                }
            };
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    public async ValueTask<OppoResult<string>> SecondaryAudioProgramAsync(CancellationToken cancellationToken = default)
    {
        if (!await _semaphore.WaitAsync(_timeout, cancellationToken))
            return false;
        
        try
        {
            var result = await SendCommand(SecondaryAudioProgram);
        
            return result.Success switch
            {
                false => new OppoResult<string> { Success = false },
                _ => new OppoResult<string>
                {
                    Success = true,
                    Result = result.Response[4..]
                }
            };
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    public async ValueTask<OppoResult<ABReplayState>> ABReplayAsync(CancellationToken cancellationToken = default)
    {
        if (!await _semaphore.WaitAsync(_timeout, cancellationToken))
            return false;
        
        try
        {
            var result = await SendCommand(ABReplay);
        
            return result.Success switch
            {
                false => new OppoResult<ABReplayState> { Success = false },
                _ => new OppoResult<ABReplayState>
                {
                    Success = true,
                    Result = result.Response switch
                    {
                        "@OK A-\r" => ABReplayState.A,
                        "@OK AB\r" => ABReplayState.AB,
                        "@OK OFF\r" => ABReplayState.Off,
                        _ => LogError(result.Response, ABReplayState.Unknown)
                    }
                }
            };
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    public async ValueTask<OppoResult<RepeatState>> RepeatAsync(CancellationToken cancellationToken = default)
    {
        if (!await _semaphore.WaitAsync(_timeout, cancellationToken))
            return false;
        
        try
        {
            var result = await SendCommand(Repeat);
        
            return result.Success switch
            {
                false => new OppoResult<RepeatState> { Success = false },
                _ => new OppoResult<RepeatState>
                {
                    Success = true,
                    Result = result.Response switch
                    {
                        "@OK Repeat Chapter-\r" => RepeatState.RepeatChapter,
                        "@OK Repeat Title\r" => RepeatState.RepeatTitle,
                        "@OK OFF\r" => RepeatState.Off,
                        _ => LogError(result.Response, RepeatState.Unknown)
                    }
                }
            };
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    public async ValueTask<OppoResult<string>> PictureInPictureAsync(CancellationToken cancellationToken = default)
    {
        if (!await _semaphore.WaitAsync(_timeout, cancellationToken))
            return false;
        
        try
        {
            var result = await SendCommand(PictureInPicture);
        
            return result.Success switch
            {
                false => new OppoResult<string> { Success = false },
                _ => new OppoResult<string>
                {
                    Success = true,
                    Result = result.Response[4..]
                }
            };
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    public async ValueTask<bool> ResolutionAsync(CancellationToken cancellationToken = default)
    {
        if (!await _semaphore.WaitAsync(_timeout, cancellationToken))
            return false;
        
        try
        {
            var result = await SendCommand(Resolution);
            return result.Success;
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    public async ValueTask<bool> SubtitleHoldAsync(CancellationToken cancellationToken = default)
    {
        if (!await _semaphore.WaitAsync(_timeout, cancellationToken))
            return false;
        
        try
        {
            var result = await SendCommand(SubtitleHold);
            return result.Success;
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    public async ValueTask<bool> OptionAsync(CancellationToken cancellationToken = default)
    {
        if (!await _semaphore.WaitAsync(_timeout, cancellationToken))
            return false;
        
        try
        {
            var result = await SendCommand(Option);
            return result.Success;
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    public async ValueTask<bool> ThreeDAsync(CancellationToken cancellationToken = default)
    {
        if (!await _semaphore.WaitAsync(_timeout, cancellationToken))
            return false;
        
        try
        {
            var result = await SendCommand(ThreeD);
            return result.Success;
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    public async ValueTask<bool> PictureAdjustmentAsync(CancellationToken cancellationToken = default)
    {
        if (!await _semaphore.WaitAsync(_timeout, cancellationToken))
            return false;
        
        try
        {
            var result = await SendCommand(PictureAdjustment);
            return result.Success;
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    public async ValueTask<bool> HDRAsync(CancellationToken cancellationToken = default)
    {
        if (!await _semaphore.WaitAsync(_timeout, cancellationToken))
            return false;
        
        try
        {
            var result = await SendCommand(HDR);
            return result.Success;
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    public async ValueTask<bool> InfoHoldAsync(CancellationToken cancellationToken = default)
    {
        if (!await _semaphore.WaitAsync(_timeout, cancellationToken))
            return false;
        
        try
        {
            var result = await SendCommand(InfoHold);
            return result.Success;
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    public async ValueTask<bool> ResolutionHoldAsync(CancellationToken cancellationToken = default)
    {
        if (!await _semaphore.WaitAsync(_timeout, cancellationToken))
            return false;
        
        try
        {
            var result = await SendCommand(ResolutionHold);
            return result.Success;
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    public async ValueTask<bool> AVSyncAsync(CancellationToken cancellationToken = default)
    {
        if (!await _semaphore.WaitAsync(_timeout, cancellationToken))
            return false;
        
        try
        {
            var result = await SendCommand(AVSync);
            return result.Success;
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    public async ValueTask<bool> GaplessPlayAsync(CancellationToken cancellationToken = default)
    {
        if (!await _semaphore.WaitAsync(_timeout, cancellationToken))
            return false;
        
        try
        {
            var result = await SendCommand(GaplessPlay);
            return result.Success;
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    public async ValueTask<bool> NoopAsync(CancellationToken cancellationToken = default)
    {
        if (!await _semaphore.WaitAsync(_timeout, cancellationToken))
            return false;
        
        try
        {
            var result = await SendCommand(Noop);
            return result.Success;
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    public async ValueTask<bool> InputAsync(CancellationToken cancellationToken = default)
    {
        if (!await _semaphore.WaitAsync(_timeout, cancellationToken))
            return false;
        
        try
        {
            var result = await SendCommand(Input);
            return result.Success;
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    public async ValueTask<OppoResult<RepeatMode>> SetRepeatAsync(RepeatMode mode, CancellationToken cancellationToken = default)
    {
        if (mode == RepeatMode.Unknown)
            return false;
        
        if (!await _semaphore.WaitAsync(_timeout, cancellationToken))
            return false;
        
        try
        {
            var result = await SendCommand(mode switch
            {
                RepeatMode.Chapter => SetRepeatModeChapter,
                RepeatMode.Title => SetRepeatModeTitle,
                RepeatMode.All => SetRepeatModeAll,
                RepeatMode.Off => SetRepeatModeOff,
                RepeatMode.Shuffle => SetRepeatModeShuffle,
                RepeatMode.Random => SetRepeatModeRandom,
                _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown repeat mode")
            });
        
            return result.Success switch
            {
                false => new OppoResult<RepeatMode> { Success = false },
                _ => new OppoResult<RepeatMode>
                {
                    Success = true,
                    Result = result.Response switch
                    {
                        "@OK CH\r" => RepeatMode.Chapter,
                        "@OK TT\r" => RepeatMode.Title,
                        "@OK ALL\r" => RepeatMode.All,
                        "@OK OFF\r" => RepeatMode.Off,
                        "@OK SHF\r" => RepeatMode.Shuffle,
                        "@OK RND\r" => RepeatMode.Random,
                        _ => LogError(result.Response, RepeatMode.Unknown)
                    }
                }
            };
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async ValueTask<OppoResult<ushort>> SetVolumeAsync([Range(0, 100)] ushort volume, CancellationToken cancellationToken = default)
    {
        if (volume > 100)
            return false;
        
        if (!await _semaphore.WaitAsync(_timeout, cancellationToken))
            return false;

        try
        {
            var result = await SendCommand(Encoding.UTF8.GetBytes($"#SVL {volume}\r"));
        
            return result.Success switch
            {
                false => new OppoResult<ushort> { Success = false },
                _ => new OppoResult<ushort>
                {
                    Success = ushort.TryParse(result.Response.AsSpan()[4..^1], out var newVolume),
                    Result = newVolume
                }
            };
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async ValueTask<OppoResult<PowerState>> QueryPowerStatusAsync(CancellationToken cancellationToken = default)
    {
        if (!await _semaphore.WaitAsync(_timeout, cancellationToken))
            return false;
        
        try
        {
            var result = await SendCommand(QueryPowerStatus);
        
            return result.Success switch
            {
                false => false,
                _ => new OppoResult<PowerState>
                {
                    Success = true,
                    Result = result.Response switch
                    {
                        "@OK ON\r" => PowerState.On,
                        "@OK OFF\r" => PowerState.Off,
                        _ => LogError(result.Response, PowerState.Unknown)
                    }
                }
            };
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async ValueTask<OppoResult<PlaybackStatus>> QueryPlaybackStatusAsync(CancellationToken cancellationToken = default)
    {
        if (!await _semaphore.WaitAsync(_timeout, cancellationToken))
            return false;
        
        try
        {
            var result = await SendCommand(QueryPlaybackStatus);
        
            return result.Success switch
            {
                false => false,
                _ => new OppoResult<PlaybackStatus>
                {
                    Success = true,
                    Result = result.Response switch
                    {
                        "@OK PLAY\r" => PlaybackStatus.Play,
                        "@OK PAUSE\r" => PlaybackStatus.Pause,
                        "@OK STOP\r" => PlaybackStatus.Stop,
                        "@OK STEP\r" => PlaybackStatus.Step,
                        "@OK FREV\r" => PlaybackStatus.FastRewind,
                        "@OK FFWD\r" => PlaybackStatus.FastForward,
                        "@OK SFWD\r" => PlaybackStatus.SlowForward,
                        "@OK SREV\r" => PlaybackStatus.SlowRewind,
                        "@OK SETUP\r" => PlaybackStatus.Setup,
                        "@OK HOME MENU\r" => PlaybackStatus.HomeMenu,
                        "@OK MEDIA CENTER\r" => PlaybackStatus.MediaCenter,
                        "@OK SCREEN SAVER\r" => PlaybackStatus.ScreenSaver,
                        "@OK DISC MENU\r" => PlaybackStatus.DiscMenu,
                        _ => LogError(result.Response, PlaybackStatus.Unknown)
                    }
                }
            };
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public ValueTask<OppoResult<uint>> QueryTrackOrTitleElapsedTimeAsync(CancellationToken cancellationToken = default)
        => QueryTimeAsync(QueryTrackOrTitleElapsedTime, cancellationToken);

    public ValueTask<OppoResult<uint>> QueryTrackOrTitleRemainingTimeAsync(CancellationToken cancellationToken = default)
        => QueryTimeAsync(QueryTrackOrTitleRemainingTime, cancellationToken);

    public ValueTask<OppoResult<uint>> QueryChapterElapsedTimeAsync(CancellationToken cancellationToken = default)
        => QueryTimeAsync(QueryChapterElapsedTime, cancellationToken);

    public ValueTask<OppoResult<uint>> QueryChapterRemainingTimeAsync(CancellationToken cancellationToken = default)
        => QueryTimeAsync(QueryChapterRemainingTime, cancellationToken);

    public ValueTask<OppoResult<uint>> QueryTotalElapsedTimeAsync(CancellationToken cancellationToken = default)
        => QueryTimeAsync(QueryTotalElapsedTime, cancellationToken);

    public ValueTask<OppoResult<uint>> QueryTotalRemainingTimeAsync(CancellationToken cancellationToken = default)
        => QueryTimeAsync(QueryTotalRemainingTime, cancellationToken);

    public async ValueTask<OppoResult<DiscType>> QueryDiscTypeAsync(CancellationToken cancellationToken = default)
    {
        if (!await _semaphore.WaitAsync(_timeout, cancellationToken))
            return false;

        try
        {
            var result = await SendCommand(QueryDiscType);
        
            return result.Success switch
            {
                false => false,
                _ => new OppoResult<DiscType>
                {
                    Success = true,
                    Result = result.Response switch
                    {
                        "@OK BD-MV\r" => DiscType.BlueRayMovie,
                        "@OK DVD-AUDIO\r" => DiscType.DVDAudio,
                        "@OK DVD-VIDEO\r" => DiscType.DVDVideo,
                        "@OK SACD\r" => DiscType.SACD,
                        "@OK CDDA\r" => DiscType.CDDiscAudio,
                        "@OK DADA-DISC\r" => DiscType.DataDisc,
                        "@OK UHBD\r" => DiscType.UltraHDBluRay,
                        "@OK NO-DISC\r" => DiscType.NoDisc,
                        "@OK UNKNOWN-DISC\r" => DiscType.UnknownDisc,
                        _ => LogError(result.Response, DiscType.UnknownDisc)
                    }
                }
            };
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async ValueTask<OppoResult<CurrentRepeatMode>> QueryRepeatModeAsync(CancellationToken cancellationToken = default)
    {
        if (!await _semaphore.WaitAsync(_timeout, cancellationToken))
            return false;

        try
        {
            var result = await SendCommand(QueryRepeatMode);
        
            return result.Success switch
            {
                false => false,
                _ => new OppoResult<CurrentRepeatMode>
                {
                    Success = true,
                    Result = result.Response switch
                    {
                        "@OK 00 Off\r" => CurrentRepeatMode.Off,
                        "@OK 01 Repeat One\r" => CurrentRepeatMode.RepeatOne,
                        "@OK 02 Repeat Chapter\r" => CurrentRepeatMode.RepeatChapter,
                        "@OK 03 Repeat All\r" => CurrentRepeatMode.RepeatAll,
                        "@OK 04 Repeat Title\r" => CurrentRepeatMode.RepeatTitle,
                        "@OK 05 Shuffle\r" => CurrentRepeatMode.Shuffle,
                        "@OK 06 Random\r" => CurrentRepeatMode.Random,
                        _ => LogError(result.Response, CurrentRepeatMode.Unknown)
                    }
                }
            };
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    public async ValueTask<OppoResult<string>> QueryCDDBNumberAsync(CancellationToken cancellationToken = default)
    {
        if (!await _semaphore.WaitAsync(_timeout, cancellationToken))
            return false;
        
        try
        {
            var result = await SendCommand(QueryCDDBNumber);
        
            return result.Success switch
            {
                false => false,
                _ => new OppoResult<string>
                {
                    Success = true,
                    Result = result.Response[4..^1]
                }
            };
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async ValueTask<OppoResult<string>> QueryTrackNameAsync(CancellationToken cancellationToken = default)
    {
        if (!await _semaphore.WaitAsync(_timeout, cancellationToken))
            return false;
        
        try
        {
            var result = await SendCommand(QueryTrackName);
        
            return result.Success switch
            {
                false => false,
                _ => new OppoResult<string>
                {
                    Success = true,
                    Result = result.Response[4..^1]
                }
            };
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async ValueTask<OppoResult<string>> QueryTrackAlbumAsync(CancellationToken cancellationToken = default)
    {
        if (!await _semaphore.WaitAsync(_timeout, cancellationToken))
            return false;
        
        try
        {
            var result = await SendCommand(QueryTrackAlbum);
        
            return result.Success switch
            {
                false => false,
                _ => new OppoResult<string>
                {
                    Success = true,
                    Result = result.Response[4..^1]
                }
            };
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async ValueTask<OppoResult<string>> QueryTrackPerformerAsync(CancellationToken cancellationToken = default)
    {
        if (!await _semaphore.WaitAsync(_timeout, cancellationToken))
            return false;
        
        try
        {
            var result = await SendCommand(QueryTrackPerformer);
        
            return result.Success switch
            {
                false => false,
                _ => new OppoResult<string>
                {
                    Success = true,
                    Result = result.Response[4..^1]
                }
            };
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public bool IsConnected => _client.IsConnected;
    
    public string GetHost() => _hostName;

    private async ValueTask<OppoResultCore> SendCommand(byte[] command, [CallerMemberName] string? caller = null)
    {
        try
        {
            if (_logger.IsEnabled(LogLevel.Trace))
            {
                _logger.LogTrace("Sending command '{Command}'", Encoding.UTF8.GetString(command));
            }
            
            await _client.WriteAsync(command);
            var response = await _client.ReadAsync();
            if (_logger.IsEnabled(LogLevel.Trace))
            {
                _logger.LogTrace("Received response '{Response}'", response);
            }

            if (response is { Length: >= 3 } && response.AsSpan()[..3] is "@OK")
                return new OppoResultCore(true, response);

            if (response is { Length: 0 })
            {
                _logger.LogDebug("{Caller} - Command not valid at this time", caller);
                return new OppoResultCore(false, null);
            }
            
            _logger.LogError("{Caller} - Failed to send command. Response was '{Response}'", caller, response);
            
            return new OppoResultCore(false, response);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to send command");
            return new OppoResultCore(false, null);
        }
    }

    private readonly record struct OppoResultCore(
        bool Success,
        string? Response)
    {
        public string? Response { get; } = Response;

        [MemberNotNullWhen(true, nameof(Response))]
        public bool Success { get; } = Success;
    }
    
    private TEnum LogError<TEnum>(string response, TEnum returnValue, [CallerMemberName]string? callerMemberName = null)
        where TEnum : Enum
    {
        _logger.LogError("{CallerMemberName} failed. Response was {Response}", callerMemberName, response);
        return returnValue;
    }
    
    private async ValueTask<OppoResult<uint>> QueryTimeAsync(byte[] command, CancellationToken cancellationToken)
    {
        if (!await _semaphore.WaitAsync(_timeout, cancellationToken))
            return false;
        
        try
        {
            var result = await SendCommand(command);
        
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
        finally
        {
            _semaphore.Release();
        }
    }
    
    private static uint ParseTime(in ReadOnlySpan<char> response)
    {
        var time = response[4..^1];
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
        _byteStream.Dispose();
        _client.Dispose();
        _semaphore.Dispose();
    }
}