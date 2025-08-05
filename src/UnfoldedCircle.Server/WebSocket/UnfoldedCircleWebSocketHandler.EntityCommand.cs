using Oppo;

using UnfoldedCircle.Models.Events;
using UnfoldedCircle.Models.Sync;
using UnfoldedCircle.Server.Oppo;
using UnfoldedCircle.Server.Response;

namespace UnfoldedCircle.Server.WebSocket;

internal sealed partial class UnfoldedCircleWebSocketHandler
{
    private async Task HandleEntityCommand(
        System.Net.WebSockets.WebSocket socket,
        EntityCommandMsg<OppoCommandId> payload,
        string wsId,
        string entityId,
        CancellationTokenWrapper cancellationTokenWrapper)
    {
        var oppoClientHolder = await TryGetOppoClientHolder(wsId, entityId, IdentifierType.EntityId, cancellationTokenWrapper.ApplicationStopping);
        if (oppoClientHolder is null || !await oppoClientHolder.Client.IsConnectedAsync())
        {
            await SendAsync(socket,
                ResponsePayloadHelpers.CreateValidationErrorResponsePayload(payload,
                    new ValidationError
                    {
                        Code = "INV_ARGUMENT",
                        Message = oppoClientHolder is null ? "Device not found" : "Device not connected"
                    },
                    _unfoldedCircleJsonSerializerContext),
                wsId,
                cancellationTokenWrapper.ApplicationStopping);
            return;
        }
        
        var success = true;
        switch (payload.MsgData.CommandId)
        {
            case OppoCommandId.On:
                cancellationTokenWrapper.EnsureNonCancelledBroadcastCancellationTokenSource();
                var powerStateResponse = await oppoClientHolder.Client.PowerOnAsync(cancellationTokenWrapper.ApplicationStopping);
                // Power commands can be flaky, so we try twice
                if (powerStateResponse is not { Result: PowerState.On })
                    powerStateResponse = await oppoClientHolder.Client.PowerOnAsync(cancellationTokenWrapper.ApplicationStopping);

                if (powerStateResponse is not { Result: PowerState.Unknown })
                    // Run in background
                    _ = HandleEventUpdates(socket, wsId, oppoClientHolder, cancellationTokenWrapper);
                
                break;
            case OppoCommandId.Off:
                await (cancellationTokenWrapper.GetCurrentBroadcastCancellationTokenSource()?.CancelAsync() ?? Task.CompletedTask);
                
                // Power commands can be flaky, so we try twice
                if ((await oppoClientHolder.Client.PowerOffAsync(cancellationTokenWrapper.ApplicationStopping)) is not { Result: PowerState.Off })
                    await oppoClientHolder.Client.PowerOffAsync(cancellationTokenWrapper.ApplicationStopping);
                
                await SendAsync(socket,
                    ResponsePayloadHelpers.CreateStateChangedResponsePayload(
                        new StateChangedEventMessageDataAttributes { State = State.Off },
                        entityId,
                        _unfoldedCircleJsonSerializerContext),
                    wsId,
                    cancellationTokenWrapper.ApplicationStopping);
                break;
            case OppoCommandId.Toggle:
                await oppoClientHolder.Client.PowerToggleAsync(cancellationTokenWrapper.ApplicationStopping);
                break;
            case OppoCommandId.PlayPause:
                await oppoClientHolder.Client.PauseAsync(cancellationTokenWrapper.ApplicationStopping);
                break;
            case OppoCommandId.Stop:
                await oppoClientHolder.Client.StopAsync(cancellationTokenWrapper.ApplicationStopping);
                break;
            case OppoCommandId.Previous:
                await oppoClientHolder.Client.PreviousAsync(cancellationTokenWrapper.ApplicationStopping);
                break;
            case OppoCommandId.Next:
                await oppoClientHolder.Client.NextAsync(cancellationTokenWrapper.ApplicationStopping);
                break;
            case OppoCommandId.FastForward:
                await oppoClientHolder.Client.ForwardAsync(cancellationTokenWrapper.ApplicationStopping);
                break;
            case OppoCommandId.Rewind:
                await oppoClientHolder.Client.ReverseAsync(cancellationTokenWrapper.ApplicationStopping);
                break;
            case OppoCommandId.Seek:
                if (payload.MsgData.Params is { MediaPosition: not null })
                {
                    var digits = GetDigits(payload.MsgData.Params.MediaPosition.Value);
                    await oppoClientHolder.Client.GoToAsync(cancellationTokenWrapper.ApplicationStopping);
                    foreach (uint digit in digits)
                    {
                        if (digit > 9)
                        {
                            await oppoClientHolder.Client.ClearAsync(cancellationTokenWrapper.ApplicationStopping);
                            await oppoClientHolder.Client.EnterAsync(cancellationTokenWrapper.ApplicationStopping);
                            return;
                        }
                        
                        await oppoClientHolder.Client.NumericInput((ushort)digit, cancellationTokenWrapper.ApplicationStopping);
                    }

                    await oppoClientHolder.Client.EnterAsync(cancellationTokenWrapper.ApplicationStopping);
                }
                break;
            case OppoCommandId.VolumeUp:
                await oppoClientHolder.Client.VolumeUpAsync(cancellationTokenWrapper.ApplicationStopping);
                break;
            case OppoCommandId.VolumeDown:
                await oppoClientHolder.Client.VolumeDownAsync(cancellationTokenWrapper.ApplicationStopping);
                break;
            case OppoCommandId.MuteToggle:
                await oppoClientHolder.Client.MuteToggleAsync(cancellationTokenWrapper.ApplicationStopping);
                break;
            case OppoCommandId.Repeat:
                if (payload.MsgData.Params is { Repeat: not null })
                    await oppoClientHolder.Client.SetRepeatAsync(payload.MsgData.Params.Repeat switch
                    {
                        Models.Shared.RepeatMode.Off => RepeatMode.Off,
                        Models.Shared.RepeatMode.All => RepeatMode.All,
                        Models.Shared.RepeatMode.One => RepeatMode.Title,
                        _ => RepeatMode.Off
                    }, cancellationTokenWrapper.ApplicationStopping);
                else
                    await oppoClientHolder.Client.RepeatAsync(cancellationTokenWrapper.ApplicationStopping);
                
                break;
            case OppoCommandId.ChannelUp:
                await oppoClientHolder.Client.PageUpAsync(cancellationTokenWrapper.ApplicationStopping);
                break;
            case OppoCommandId.ChannelDown:
                await oppoClientHolder.Client.PageDownAsync(cancellationTokenWrapper.ApplicationStopping);
                break;
            case OppoCommandId.CursorUp:
                await oppoClientHolder.Client.UpArrowAsync(cancellationTokenWrapper.ApplicationStopping);
                break;
            case OppoCommandId.CursorDown:
                await oppoClientHolder.Client.DownArrowAsync(cancellationTokenWrapper.ApplicationStopping);
                break;
            case OppoCommandId.CursorLeft:
                await oppoClientHolder.Client.LeftArrowAsync(cancellationTokenWrapper.ApplicationStopping);
                break;
            case OppoCommandId.CursorRight:
                await oppoClientHolder.Client.RightArrowAsync(cancellationTokenWrapper.ApplicationStopping);
                break;
            case OppoCommandId.CursorEnter:
                await oppoClientHolder.Client.EnterAsync(cancellationTokenWrapper.ApplicationStopping);
                break;
            case OppoCommandId.Digit0:
                await oppoClientHolder.Client.NumericInput(0, cancellationTokenWrapper.ApplicationStopping);
                break;
            case OppoCommandId.Digit1:
                await oppoClientHolder.Client.NumericInput(1, cancellationTokenWrapper.ApplicationStopping);
                break;
            case OppoCommandId.Digit2:
                await oppoClientHolder.Client.NumericInput(2, cancellationTokenWrapper.ApplicationStopping);
                break;
            case OppoCommandId.Digit3:
                await oppoClientHolder.Client.NumericInput(3, cancellationTokenWrapper.ApplicationStopping);
                break;
            case OppoCommandId.Digit4:
                await oppoClientHolder.Client.NumericInput(4, cancellationTokenWrapper.ApplicationStopping);
                break;
            case OppoCommandId.Digit5:
                await oppoClientHolder.Client.NumericInput(5, cancellationTokenWrapper.ApplicationStopping);
                break;
            case OppoCommandId.Digit6:
                await oppoClientHolder.Client.NumericInput(6, cancellationTokenWrapper.ApplicationStopping);
                break;
            case OppoCommandId.Digit7:
                await oppoClientHolder.Client.NumericInput(7, cancellationTokenWrapper.ApplicationStopping);
                break;
            case OppoCommandId.Digit8:
                await oppoClientHolder.Client.NumericInput(8, cancellationTokenWrapper.ApplicationStopping);
                break;
            case OppoCommandId.Digit9:
                await oppoClientHolder.Client.NumericInput(9, cancellationTokenWrapper.ApplicationStopping);
                break;
            case OppoCommandId.FunctionRed:
                await oppoClientHolder.Client.RedAsync(cancellationTokenWrapper.ApplicationStopping);
                break;
            case OppoCommandId.FunctionGreen:
                await oppoClientHolder.Client.GreenAsync(cancellationTokenWrapper.ApplicationStopping);
                break;
            case OppoCommandId.FunctionYellow:
                await oppoClientHolder.Client.YellowAsync(cancellationTokenWrapper.ApplicationStopping);
                break;
            case OppoCommandId.FunctionBlue:
                await oppoClientHolder.Client.BlueAsync(cancellationTokenWrapper.ApplicationStopping);
                break;
            case OppoCommandId.Home:
                await oppoClientHolder.Client.HomeAsync(cancellationTokenWrapper.ApplicationStopping);
                break;
            case OppoCommandId.ContextMenu:
                await oppoClientHolder.Client.TopMenuAsync(cancellationTokenWrapper.ApplicationStopping);
                break;
            case OppoCommandId.Info:
                await oppoClientHolder.Client.InfoToggleAsync(cancellationTokenWrapper.ApplicationStopping);
                break;
            case OppoCommandId.Back:
                await oppoClientHolder.Client.ReturnAsync(cancellationTokenWrapper.ApplicationStopping);
                break;
            case OppoCommandId.SelectSource:
                if (payload.MsgData.Params is { Source: not null } && OppoEntitySettings.SourceMap.TryGetValue(payload.MsgData.Params.Source, out var source))
                {
                    // Sending input source is only allowed if the unit is on - avoid locking up the driver by only sending it when the unit is ready
                    var powerState = await oppoClientHolder.Client.QueryPowerStatusAsync(cancellationTokenWrapper.ApplicationStopping);
                    if (powerState is { Result: PowerState.On })
                        await oppoClientHolder.Client.SetInputSourceAsync(source, cancellationTokenWrapper.ApplicationStopping);
                }
                else
                    await oppoClientHolder.Client.InputAsync(cancellationTokenWrapper.ApplicationStopping);
                break;
            case OppoCommandId.PureAudioToggle:
                await oppoClientHolder.Client.PureAudioToggleAsync(cancellationTokenWrapper.ApplicationStopping);
                break;
            case OppoCommandId.OpenClose:
                await oppoClientHolder.Client.EjectToggleAsync(cancellationTokenWrapper.ApplicationStopping);
                break;
            case OppoCommandId.AudioTrack:
                await oppoClientHolder.Client.AudioAsync(cancellationTokenWrapper.ApplicationStopping);
                break;
            case OppoCommandId.Subtitle:
                await oppoClientHolder.Client.SubtitleAsync(cancellationTokenWrapper.ApplicationStopping);
                break;
            case OppoCommandId.Settings:
                await oppoClientHolder.Client.SetupAsync(cancellationTokenWrapper.ApplicationStopping);
                break;
            case OppoCommandId.Dimmer:
                await oppoClientHolder.Client.DimmerAsync(cancellationTokenWrapper.ApplicationStopping);
                break;
            case OppoCommandId.Clear:
                await oppoClientHolder.Client.ClearAsync(cancellationTokenWrapper.ApplicationStopping);
                break;
            case OppoCommandId.PopUpMenu:
                await oppoClientHolder.Client.PopUpMenuAsync(cancellationTokenWrapper.ApplicationStopping);
                break;
            case OppoCommandId.Pause:
                await oppoClientHolder.Client.PauseAsync(cancellationTokenWrapper.ApplicationStopping);
                break;
            case OppoCommandId.Play:
                await oppoClientHolder.Client.PlayAsync(cancellationTokenWrapper.ApplicationStopping);
                break;
            case OppoCommandId.Angle:
                await oppoClientHolder.Client.AngleAsync(cancellationTokenWrapper.ApplicationStopping);
                break;
            case OppoCommandId.Zoom:
                await oppoClientHolder.Client.ZoomAsync(cancellationTokenWrapper.ApplicationStopping);
                break;
            case OppoCommandId.SecondaryAudioProgram:
                await oppoClientHolder.Client.SecondaryAudioProgramAsync(cancellationTokenWrapper.ApplicationStopping);
                break;
            case OppoCommandId.AbReplay:
                await oppoClientHolder.Client.ABReplayAsync(cancellationTokenWrapper.ApplicationStopping);
                break;
            case OppoCommandId.PictureInPicture:
                await oppoClientHolder.Client.PictureInPictureAsync(cancellationTokenWrapper.ApplicationStopping);
                break;
            case OppoCommandId.Resolution:
                await oppoClientHolder.Client.ResolutionAsync(cancellationTokenWrapper.ApplicationStopping);
                break;
            case OppoCommandId.SubtitleHold:
                await oppoClientHolder.Client.SubtitleHoldAsync(cancellationTokenWrapper.ApplicationStopping);
                break;
            case OppoCommandId.Option:
                await oppoClientHolder.Client.OptionAsync(cancellationTokenWrapper.ApplicationStopping);
                break;
            case OppoCommandId.ThreeD:
                await oppoClientHolder.Client.ThreeDAsync(cancellationTokenWrapper.ApplicationStopping);
                break;
            case OppoCommandId.PictureAdjustment:
                await oppoClientHolder.Client.PictureAdjustmentAsync(cancellationTokenWrapper.ApplicationStopping);
                break;
            case OppoCommandId.Hdr:
                await oppoClientHolder.Client.HDRAsync(cancellationTokenWrapper.ApplicationStopping);
                break;
            case OppoCommandId.InfoHold:
                await oppoClientHolder.Client.InfoHoldAsync(cancellationTokenWrapper.ApplicationStopping);
                break;
            case OppoCommandId.ResolutionHold:
                await oppoClientHolder.Client.ResolutionHoldAsync(cancellationTokenWrapper.ApplicationStopping);
                break;
            case OppoCommandId.AvSync:
                await oppoClientHolder.Client.AVSyncAsync(cancellationTokenWrapper.ApplicationStopping);
                break;
            case OppoCommandId.GaplessPlay:
                await oppoClientHolder.Client.GaplessPlayAsync(cancellationTokenWrapper.ApplicationStopping);
                break;

            case OppoCommandId.Shuffle:
                if (payload.MsgData.Params is { Shuffle: not null })
                    await oppoClientHolder.Client.SetRepeatAsync(payload.MsgData.Params.Shuffle.Value ? RepeatMode.Shuffle : RepeatMode.Off, cancellationTokenWrapper.ApplicationStopping);
                break;
            
            case OppoCommandId.Volume:
                if (payload.MsgData.Params is { Volume: not null })
                    await oppoClientHolder.Client.SetVolumeAsync(payload.MsgData.Params.Volume.Value, cancellationTokenWrapper.ApplicationStopping);
                break;
            
            // unsupported default commands
            case OppoCommandId.Mute:
            case OppoCommandId.Unmute:
            case OppoCommandId.Menu:
            case OppoCommandId.Guide:
            case OppoCommandId.Record:
            case OppoCommandId.MyRecordings:
            case OppoCommandId.Live:
            case OppoCommandId.Eject:
            case OppoCommandId.Search:
            default:
                success = false;
                break;
        }
        
        if (success)
        {
            await SendAsync(socket,
                ResponsePayloadHelpers.CreateCommonResponsePayload(payload, _unfoldedCircleJsonSerializerContext),
                wsId,
                cancellationTokenWrapper.ApplicationStopping);
        }
        else
        {
            await SendAsync(socket,
                ResponsePayloadHelpers.CreateValidationErrorResponsePayload(payload,
                    new ValidationError
                    {
                        Code = "INV_ARGUMENT",
                        Message = "Unknown command"
                    },
                    _unfoldedCircleJsonSerializerContext),
                wsId,
                cancellationTokenWrapper.ApplicationStopping);
        }
    }
    
    private static List<uint> GetDigits(uint number)
    {
        var digits = new List<uint>();
        while (number > 0)
        {
            digits.Add(number % 10);
            number /= 10;
        }
        digits.Reverse();
        return digits;
    }
}