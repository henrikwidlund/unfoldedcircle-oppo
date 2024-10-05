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
    private readonly TcpClient _tcpClient = new();
    private readonly string _hostName = hostName;
    private readonly int _port = (ushort)model;
    private readonly OppoModel _model = model;
    private readonly ILogger<OppoClient> _logger = logger;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly TimeSpan _timeout = TimeSpan.FromSeconds(1);
    private readonly StringBuilder _stringBuilder = new();

    public async ValueTask<OppoResult<PowerState>> PowerToggleAsync(CancellationToken cancellationToken = default)
    {
        if (!await _semaphore.WaitAsync(_timeout, cancellationToken))
            return false;
        
        try
        {
            var command = _model == OppoModel.UDP20X ? Oppo20XCommand.PowerToggle : Oppo10XCommand.PowerToggle;
            var result = await SendCommand(command, cancellationToken);
            return result.Success switch
            {
                false => new OppoResult<PowerState> { Success = false },
                _ => new OppoResult<PowerState>
                {
                    Success = true,
                    Result = result.Response switch
                    {
                        "@OK ON" => PowerState.On,
                        "@OK OFF" => PowerState.Off,
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
            var command = _model == OppoModel.UDP20X ? Oppo20XCommand.PowerOn : Oppo10XCommand.PowerOn;
            var result = await SendCommand(command, cancellationToken);

            return result.Success switch
            {
                false => new OppoResult<PowerState> { Success = false },
                _ => new OppoResult<PowerState>
                {
                    Success = true,
                    Result = result.Response switch
                    {
                        "@OK ON" => PowerState.On,
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
            var command = _model == OppoModel.UDP20X ? Oppo20XCommand.PowerOff : Oppo10XCommand.PowerOff;
            var result = await SendCommand(command, cancellationToken);

            return result.Success switch
            {
                false => new OppoResult<PowerState> { Success = false },
                _ => new OppoResult<PowerState>
                {
                    Success = true,
                    Result = result.Response switch
                    {
                        "@OK OFF" => PowerState.Off,
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
            var command = _model == OppoModel.UDP20X ? Oppo20XCommand.EjectToggle : Oppo10XCommand.EjectToggle;
            var result = await SendCommand(command, cancellationToken);

            return result.Success switch
            {
                false => new OppoResult<TrayState> { Success = false },
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
            var command = _model == OppoModel.UDP20X ? Oppo20XCommand.Dimmer : Oppo10XCommand.Dimmer;
            var result = await SendCommand(command, cancellationToken);

            return result.Success switch
            {
                false => new OppoResult<DimmerState> { Success = false },
                _ => new OppoResult<DimmerState>
                {
                    Success = true,
                    Result = result.Response switch
                    {
                        "@OK ON" => DimmerState.On,
                        "@OK DIM" => DimmerState.Dim,
                        "@OK OFF" => DimmerState.Off,
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
            var command = _model == OppoModel.UDP20X ? Oppo20XCommand.PureAudioToggle : Oppo10XCommand.PureAudioToggle;
            var result = await SendCommand(command, cancellationToken);

            return result.Success switch
            {
                false => new OppoResult<PureAudioState> { Success = false },
                _ => new OppoResult<PureAudioState>
                {
                    Success = true,
                    Result = result.Response switch
                    {
                        "@OK ON" => PureAudioState.On,
                        "@OK OFF" => PureAudioState.Off,
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
            var command = _model == OppoModel.UDP20X ? Oppo20XCommand.VolumeUp : Oppo10XCommand.VolumeUp;
            var result = await SendCommand(command, cancellationToken);

            return result.Success switch
            {
                false => new OppoResult<ushort?> { Success = false },
                _ => new OppoResult<ushort?>
                {
                    Success = ushort.TryParse(result.Response.AsSpan()[4..], out var volume),
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
            var command = _model == OppoModel.UDP20X ? Oppo20XCommand.VolumeDown : Oppo10XCommand.VolumeDown;
            var result = await SendCommand(command, cancellationToken);

            return result.Success switch
            {
                false => new OppoResult<ushort?> { Success = false },
                _ => new OppoResult<ushort?>
                {
                    Success = ushort.TryParse(result.Response.AsSpan()[4..], out var volume),
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
            var command = _model == OppoModel.UDP20X ? Oppo20XCommand.MuteToggle : Oppo10XCommand.MuteToggle;
            var result = await SendCommand(command, cancellationToken);

            return result.Success switch
            {
                false => new OppoResult<MuteState> { Success = false },
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
                0 => _model == OppoModel.UDP20X ? Oppo20XCommand.NumericKey0 : Oppo10XCommand.NumericKey0,
                1 => _model == OppoModel.UDP20X ? Oppo20XCommand.NumericKey1 : Oppo10XCommand.NumericKey1,
                2 => _model == OppoModel.UDP20X ? Oppo20XCommand.NumericKey2 : Oppo10XCommand.NumericKey2,
                3 => _model == OppoModel.UDP20X ? Oppo20XCommand.NumericKey3 : Oppo10XCommand.NumericKey3,
                4 => _model == OppoModel.UDP20X ? Oppo20XCommand.NumericKey4 : Oppo10XCommand.NumericKey4,
                5 => _model == OppoModel.UDP20X ? Oppo20XCommand.NumericKey5 : Oppo10XCommand.NumericKey5,
                6 => _model == OppoModel.UDP20X ? Oppo20XCommand.NumericKey6 : Oppo10XCommand.NumericKey6,
                7 => _model == OppoModel.UDP20X ? Oppo20XCommand.NumericKey7 : Oppo10XCommand.NumericKey7,
                8 => _model == OppoModel.UDP20X ? Oppo20XCommand.NumericKey8 : Oppo10XCommand.NumericKey8,
                9 => _model == OppoModel.UDP20X ? Oppo20XCommand.NumericKey9 : Oppo10XCommand.NumericKey9,
                _ => throw new ArgumentOutOfRangeException(nameof(number))
            }, cancellationToken);
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
            var command = _model == OppoModel.UDP20X ? Oppo20XCommand.Clear : Oppo10XCommand.Clear;
            var result = await SendCommand(command, cancellationToken);
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
            var command = _model == OppoModel.UDP20X ? Oppo20XCommand.GoTo : Oppo10XCommand.GoTo;
            var result = await SendCommand(command, cancellationToken);
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
            var command = _model == OppoModel.UDP20X ? Oppo20XCommand.Home : Oppo10XCommand.Home;
            var result = await SendCommand(command, cancellationToken);
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
            var command = _model == OppoModel.UDP20X ? Oppo20XCommand.PageUp : Oppo10XCommand.PageUp;
            var result = await SendCommand(command, cancellationToken);
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
            var command = _model == OppoModel.UDP20X ? Oppo20XCommand.PageDown : Oppo10XCommand.PageDown;
            var result = await SendCommand(command, cancellationToken);
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
            var command = _model == OppoModel.UDP20X ? Oppo20XCommand.InfoToggle : Oppo10XCommand.InfoToggle;
            var result = await SendCommand(command, cancellationToken);
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
            var command = _model == OppoModel.UDP20X ? Oppo20XCommand.TopMenu : Oppo10XCommand.TopMenu;
            var result = await SendCommand(command, cancellationToken);
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
            var command = _model == OppoModel.UDP20X ? Oppo20XCommand.PopUpMenu : Oppo10XCommand.PopUpMenu;
            var result = await SendCommand(command, cancellationToken);
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
            var command = _model == OppoModel.UDP20X ? Oppo20XCommand.UpArrow : Oppo10XCommand.UpArrow;
            var result = await SendCommand(command, cancellationToken);
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
            var command = _model == OppoModel.UDP20X ? Oppo20XCommand.LeftArrow : Oppo10XCommand.LeftArrow;
            var result = await SendCommand(command, cancellationToken);
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
            var command = _model == OppoModel.UDP20X ? Oppo20XCommand.RightArrow : Oppo10XCommand.RightArrow;
            var result = await SendCommand(command, cancellationToken);
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
            var command = _model == OppoModel.UDP20X ? Oppo20XCommand.DownArrow : Oppo10XCommand.DownArrow;
            var result = await SendCommand(command, cancellationToken);
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
            var command = _model == OppoModel.UDP20X ? Oppo20XCommand.Enter : Oppo10XCommand.Enter;
            var result = await SendCommand(command, cancellationToken);
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
            var command = _model == OppoModel.UDP20X ? Oppo20XCommand.Setup : Oppo10XCommand.Setup;
            var result = await SendCommand(command, cancellationToken);
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
            var command = _model == OppoModel.UDP20X ? Oppo20XCommand.Return : Oppo10XCommand.Return;
            var result = await SendCommand(command, cancellationToken);
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
            var command = _model == OppoModel.UDP20X ? Oppo20XCommand.Red : Oppo10XCommand.Red;
            var result = await SendCommand(command, cancellationToken);
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
            var command = _model == OppoModel.UDP20X ? Oppo20XCommand.Green : Oppo10XCommand.Green;
            var result = await SendCommand(command, cancellationToken);
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
            var command = _model == OppoModel.UDP20X ? Oppo20XCommand.Blue : Oppo10XCommand.Blue;
            var result = await SendCommand(command, cancellationToken);
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
            var command = _model == OppoModel.UDP20X ? Oppo20XCommand.Yellow : Oppo10XCommand.Yellow;
            var result = await SendCommand(command, cancellationToken);
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
            var command = _model == OppoModel.UDP20X ? Oppo20XCommand.Stop : Oppo10XCommand.Stop;
            var result = await SendCommand(command, cancellationToken);
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
            var command = _model == OppoModel.UDP20X ? Oppo20XCommand.Play : Oppo10XCommand.Play;
            var result = await SendCommand(command, cancellationToken);
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
            var command = _model == OppoModel.UDP20X ? Oppo20XCommand.Pause : Oppo10XCommand.Pause;
            var result = await SendCommand(command, cancellationToken);
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
            var command = _model == OppoModel.UDP20X ? Oppo20XCommand.Previous : Oppo10XCommand.Previous;
            var result = await SendCommand(command, cancellationToken);
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
            var command = _model == OppoModel.UDP20X ? Oppo20XCommand.Reverse : Oppo10XCommand.Reverse;
            var result = await SendCommand(command, cancellationToken);
        
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
            var command = _model == OppoModel.UDP20X ? Oppo20XCommand.Forward : Oppo10XCommand.Forward;
            var result = await SendCommand(command, cancellationToken);
        
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
            var command = _model == OppoModel.UDP20X ? Oppo20XCommand.Next : Oppo10XCommand.Next;
            var result = await SendCommand(command, cancellationToken);
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
            var command = _model == OppoModel.UDP20X ? Oppo20XCommand.Audio : Oppo10XCommand.Audio;
            var result = await SendCommand(command, cancellationToken);
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
            var command = _model == OppoModel.UDP20X ? Oppo20XCommand.Subtitle : Oppo10XCommand.Subtitle;
            var result = await SendCommand(command, cancellationToken);
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
            var command = _model == OppoModel.UDP20X ? Oppo20XCommand.Angle : Oppo10XCommand.Angle;
            var result = await SendCommand(command, cancellationToken);
        
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
            var command = _model == OppoModel.UDP20X ? Oppo20XCommand.Zoom : Oppo10XCommand.Zoom;
            var result = await SendCommand(command, cancellationToken);
        
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
            var command = _model == OppoModel.UDP20X ? Oppo20XCommand.SecondaryAudioProgram : Oppo10XCommand.SecondaryAudioProgram;
            var result = await SendCommand(command, cancellationToken);
        
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
            var command = _model == OppoModel.UDP20X ? Oppo20XCommand.ABReplay : Oppo10XCommand.ABReplay;
            var result = await SendCommand(command, cancellationToken);
        
            return result.Success switch
            {
                false => new OppoResult<ABReplayState> { Success = false },
                _ => new OppoResult<ABReplayState>
                {
                    Success = true,
                    Result = result.Response switch
                    {
                        "@OK A-" => ABReplayState.A,
                        "@OK AB" => ABReplayState.AB,
                        "@OK OFF" => ABReplayState.Off,
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
            var command = _model == OppoModel.UDP20X ? Oppo20XCommand.Repeat : Oppo10XCommand.Repeat;
            var result = await SendCommand(command, cancellationToken);
        
            return result.Success switch
            {
                false => new OppoResult<RepeatState> { Success = false },
                _ => new OppoResult<RepeatState>
                {
                    Success = true,
                    Result = result.Response switch
                    {
                        "@OK Repeat Chapter" => RepeatState.RepeatChapter,
                        "@OK Repeat Title" => RepeatState.RepeatTitle,
                        "@OK OFF" => RepeatState.Off,
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
            var command = _model == OppoModel.UDP20X ? Oppo20XCommand.PictureInPicture : Oppo10XCommand.PictureInPicture;
            var result = await SendCommand(command, cancellationToken);
        
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
            var command = _model == OppoModel.UDP20X ? Oppo20XCommand.Resolution : Oppo10XCommand.Resolution;
            var result = await SendCommand(command, cancellationToken);
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
            var command = _model == OppoModel.UDP20X ? Oppo20XCommand.SubtitleHold : Oppo10XCommand.SubtitleHold;
            var result = await SendCommand(command, cancellationToken);
            return result.Success;
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    public async ValueTask<bool> OptionAsync(CancellationToken cancellationToken = default)
    {
        if (_model is OppoModel.BDP83)
            return false;
        
        if (!await _semaphore.WaitAsync(_timeout, cancellationToken))
            return false;
        
        try
        {
            var command = _model == OppoModel.UDP20X ? Oppo20XCommand.Option : Oppo10XCommand.Option;
            var result = await SendCommand(command, cancellationToken);
            return result.Success;
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    public async ValueTask<bool> ThreeDAsync(CancellationToken cancellationToken = default)
    {
        if (_model is OppoModel.BDP83)
            return false;
        
        if (!await _semaphore.WaitAsync(_timeout, cancellationToken))
            return false;
        
        try
        {
            var command = _model == OppoModel.UDP20X ? Oppo20XCommand.ThreeD : Oppo10XCommand.ThreeD;
            var result = await SendCommand(command, cancellationToken);
            return result.Success;
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    public async ValueTask<bool> PictureAdjustmentAsync(CancellationToken cancellationToken = default)
    {
        if (_model is OppoModel.BDP83)
            return false;
        
        if (!await _semaphore.WaitAsync(_timeout, cancellationToken))
            return false;
        
        try
        {
            var command = _model == OppoModel.UDP20X ? Oppo20XCommand.PictureAdjustment : Oppo10XCommand.PictureAdjustment;
            var result = await SendCommand(command, cancellationToken);
            return result.Success;
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    public async ValueTask<bool> HDRAsync(CancellationToken cancellationToken = default)
    {
        if (_model is not OppoModel.UDP20X)
            return false;
        
        if (!await _semaphore.WaitAsync(_timeout, cancellationToken))
            return false;
        
        try
        {
            var result = await SendCommand(Oppo20XCommand.HDR, cancellationToken);
            return result.Success;
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    public async ValueTask<bool> InfoHoldAsync(CancellationToken cancellationToken = default)
    {
        if (_model is not OppoModel.UDP20X)
            return false;
        
        if (!await _semaphore.WaitAsync(_timeout, cancellationToken))
            return false;
        
        try
        {
            var result = await SendCommand(Oppo20XCommand.InfoHold, cancellationToken);
            return result.Success;
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    public async ValueTask<bool> ResolutionHoldAsync(CancellationToken cancellationToken = default)
    {
        if (_model is not OppoModel.UDP20X)
            return false;
        
        if (!await _semaphore.WaitAsync(_timeout, cancellationToken))
            return false;
        
        try
        {
            var result = await SendCommand(Oppo20XCommand.ResolutionHold, cancellationToken);
            return result.Success;
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    public async ValueTask<bool> AVSyncAsync(CancellationToken cancellationToken = default)
    {
        if (_model is not OppoModel.UDP20X)
            return false;
        
        if (!await _semaphore.WaitAsync(_timeout, cancellationToken))
            return false;
        
        try
        {
            var result = await SendCommand(Oppo20XCommand.AVSync, cancellationToken);
            return result.Success;
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    public async ValueTask<bool> GaplessPlayAsync(CancellationToken cancellationToken = default)
    {
        if (_model is not OppoModel.UDP20X)
            return false;
        
        if (!await _semaphore.WaitAsync(_timeout, cancellationToken))
            return false;
        
        try
        {
            var result = await SendCommand(Oppo20XCommand.GaplessPlay, cancellationToken);
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
            var command = _model == OppoModel.UDP20X ? Oppo20XCommand.Noop : Oppo10XCommand.Noop;
            var result = await SendCommand(command, cancellationToken);
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
            var command = _model == OppoModel.UDP20X ? Oppo20XCommand.Input : Oppo10XCommand.Input;
            var result = await SendCommand(command, cancellationToken);
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
                RepeatMode.Chapter => _model == OppoModel.UDP20X ? Oppo20XAdvancedCommand.SetRepeatModeChapter : Oppo10XAdvancedCommand.SetRepeatModeChapter,
                RepeatMode.Title => _model == OppoModel.UDP20X ? Oppo20XAdvancedCommand.SetRepeatModeTitle : Oppo10XAdvancedCommand.SetRepeatModeTitle,
                RepeatMode.All => _model == OppoModel.UDP20X ? Oppo20XAdvancedCommand.SetRepeatModeAll : Oppo10XAdvancedCommand.SetRepeatModeAll,
                RepeatMode.Off => _model == OppoModel.UDP20X ? Oppo20XAdvancedCommand.SetRepeatModeOff : Oppo10XAdvancedCommand.SetRepeatModeOff,
                RepeatMode.Shuffle => _model == OppoModel.UDP20X ? Oppo20XAdvancedCommand.SetRepeatModeShuffle : Oppo10XAdvancedCommand.SetRepeatModeShuffle,
                RepeatMode.Random => _model == OppoModel.UDP20X ? Oppo20XAdvancedCommand.SetRepeatModeRandom : Oppo10XAdvancedCommand.SetRepeatModeRandom,
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
                        "@OK OFF" => RepeatMode.Off,
                        "@OK SHF" => RepeatMode.Shuffle,
                        "@OK RND" => RepeatMode.Random,
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
            var command = Encoding.ASCII.GetBytes(_model == OppoModel.UDP20X ? $"#SVL {volume}\r" : $"REMOTE SVL {volume}");
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
            var command = _model == OppoModel.UDP20X ? Oppo20XQueryCommand.QueryPowerStatus : Oppo10XQueryCommand.QueryPowerStatus;
            var result = await SendCommand(command, cancellationToken);
        
            return result.Success switch
            {
                false => false,
                _ => new OppoResult<PowerState>
                {
                    Success = true,
                    Result = result.Response switch
                    {
                        "@OK ON" => PowerState.On,
                        "@OK OFF" => PowerState.Off,
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
            var command = _model == OppoModel.UDP20X ? Oppo20XQueryCommand.QueryPlaybackStatus : Oppo10XQueryCommand.QueryPlaybackStatus;
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
        finally
        {
            _semaphore.Release();
        }
    }

    public ValueTask<OppoResult<uint>> QueryTrackOrTitleElapsedTimeAsync(CancellationToken cancellationToken = default)
        => QueryTimeAsync(_model == OppoModel.UDP20X ? Oppo20XQueryCommand.QueryTrackOrTitleElapsedTime : Oppo10XQueryCommand.QueryTrackOrTitleElapsedTime,
            cancellationToken);

    public ValueTask<OppoResult<uint>> QueryTrackOrTitleRemainingTimeAsync(CancellationToken cancellationToken = default)
        => QueryTimeAsync(_model == OppoModel.UDP20X ? Oppo20XQueryCommand.QueryTrackOrTitleRemainingTime : Oppo10XQueryCommand.QueryTrackOrTitleRemainingTime,
            cancellationToken);

    public ValueTask<OppoResult<uint>> QueryChapterElapsedTimeAsync(CancellationToken cancellationToken = default)
        => QueryTimeAsync(_model == OppoModel.UDP20X ? Oppo20XQueryCommand.QueryChapterElapsedTime : Oppo10XQueryCommand.QueryChapterElapsedTime,
            cancellationToken);

    public ValueTask<OppoResult<uint>> QueryChapterRemainingTimeAsync(CancellationToken cancellationToken = default)
        => QueryTimeAsync(_model == OppoModel.UDP20X ? Oppo20XQueryCommand.QueryChapterRemainingTime : Oppo10XQueryCommand.QueryChapterRemainingTime,
            cancellationToken);

    public ValueTask<OppoResult<uint>> QueryTotalElapsedTimeAsync(CancellationToken cancellationToken = default)
        => QueryTimeAsync(_model == OppoModel.UDP20X ? Oppo20XQueryCommand.QueryTotalElapsedTime : Oppo10XQueryCommand.QueryTotalElapsedTime,
            cancellationToken);

    public ValueTask<OppoResult<uint>> QueryTotalRemainingTimeAsync(CancellationToken cancellationToken = default)
        => QueryTimeAsync(_model == OppoModel.UDP20X ? Oppo20XQueryCommand.QueryTotalRemainingTime : Oppo10XQueryCommand.QueryTotalRemainingTime,
            cancellationToken);

    public async ValueTask<OppoResult<DiscType>> QueryDiscTypeAsync(CancellationToken cancellationToken = default)
    {
        if (!await _semaphore.WaitAsync(_timeout, cancellationToken))
            return false;

        try
        {
            var command = _model == OppoModel.UDP20X ? Oppo20XQueryCommand.QueryDiscType : Oppo10XQueryCommand.QueryDiscType;
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
                        "@OK UNKNOWN-DISC" => DiscType.UnknownDisc,
                        
                        // Pre 20X models
                        "@OK HDCD" => DiscType.HDCD,
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
            var command = _model == OppoModel.UDP20X ? Oppo20XQueryCommand.QueryRepeatMode : Oppo10XQueryCommand.QueryRepeatMode;
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
        finally
        {
            _semaphore.Release();
        }
    }
    
    public async ValueTask<OppoResult<string>> QueryCDDBNumberAsync(CancellationToken cancellationToken = default)
    {
        if (_model is not OppoModel.UDP20X)
            return false;
        
        if (!await _semaphore.WaitAsync(_timeout, cancellationToken))
            return false;
        
        try
        {
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
        finally
        {
            _semaphore.Release();
        }
    }

    public async ValueTask<OppoResult<string>> QueryTrackNameAsync(CancellationToken cancellationToken = default)
    {
        if (_model is not OppoModel.UDP20X)
            return false;
        
        if (!await _semaphore.WaitAsync(_timeout, cancellationToken))
            return false;
        
        try
        {
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
        finally
        {
            _semaphore.Release();
        }
    }

    public async ValueTask<OppoResult<string>> QueryTrackAlbumAsync(CancellationToken cancellationToken = default)
    {
        if (_model is not OppoModel.UDP20X)
            return false;
        
        if (!await _semaphore.WaitAsync(_timeout, cancellationToken))
            return false;
        
        try
        {
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
        finally
        {
            _semaphore.Release();
        }
    }

    public async ValueTask<OppoResult<string>> QueryTrackPerformerAsync(CancellationToken cancellationToken = default)
    {
        if (_model is not OppoModel.UDP20X)
            return false;
        
        if (!await _semaphore.WaitAsync(_timeout, cancellationToken))
            return false;
        
        try
        {
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
        finally
        {
            _semaphore.Release();
        }
    }

    public async ValueTask<bool> IsConnectedAsync(TimeSpan? timeout = null)
    {
        if (!_tcpClient.Connected)
        {
            if (await _semaphore.WaitAsync(timeout ?? TimeSpan.FromSeconds(9)) && !_tcpClient.Connected)
            {
                try
                {
                    using var cancellationTokenSource = new CancellationTokenSource(timeout ?? TimeSpan.FromSeconds(10));
                    await _tcpClient.ConnectAsync(_hostName, _port, cancellationTokenSource.Token);
                }
                catch (OperationCanceledException)
                {
                
                }
                finally
                {
                    _semaphore.Release();
                }
            }
        }
        
        return _tcpClient.Connected;
    }

    public string GetHost() => _hostName;

    private async ValueTask<OppoResultCore> SendCommand(byte[] command, CancellationToken cancellationToken, [CallerMemberName] string? caller = null)
    {
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
            if (_model != OppoModel.UDP20X && (!span.StartsWith("@OK "u8) || !span.StartsWith("@ER "u8)))
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
        finally
        {
            _semaphore.Release();
        }
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