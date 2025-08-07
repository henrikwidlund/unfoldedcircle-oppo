using Oppo;

using UnfoldedCircle.Models.Events;
using UnfoldedCircle.Models.Shared;
using UnfoldedCircle.Models.Sync;
using UnfoldedCircle.Server.Oppo;
using UnfoldedCircle.Server.Response;

using RepeatMode = Oppo.RepeatMode;

namespace UnfoldedCircle.Server.WebSocket;

internal sealed partial class UnfoldedCircleWebSocketHandler
{
    private async Task HandleEntityCommand<TCommandId, TEntityCommandParams>(
        System.Net.WebSockets.WebSocket socket,
        CommonReq<EntityCommandMsgData<TCommandId, TEntityCommandParams>> payload,
        string wsId,
        CancellationTokenWrapper cancellationTokenWrapper)
    {
        var oppoClientHolder = await TryGetOppoClientHolder(wsId, payload.MsgData.EntityId, IdentifierType.EntityId, cancellationTokenWrapper.ApplicationStopping);
        if (oppoClientHolder is null || !await oppoClientHolder.Client.IsConnectedAsync())
        {
            await SendAsync(socket,
                ResponsePayloadHelpers.CreateValidationErrorResponsePayload(payload,
                    new ValidationError
                    {
                        Code = "INV_ARGUMENT",
                        Message = oppoClientHolder is null ? "Device not found" : "Device not connected"
                    }),
                wsId,
                cancellationTokenWrapper.ApplicationStopping);
            return;
        }

        if (payload is MediaPlayerEntityCommandMsgData<OppoCommandId> mediaPlayerEntityCommandMsgData)
            await HandleMediaPlayerCommand(socket, oppoClientHolder, mediaPlayerEntityCommandMsgData, wsId, cancellationTokenWrapper);
        else if (payload is RemoteEntityCommandMsgData remoteEntityCommandMsgData)
            await HandleRemoteCommand(socket, oppoClientHolder, remoteEntityCommandMsgData, wsId, cancellationTokenWrapper);
    }

    private async Task HandleMediaPlayerCommand(System.Net.WebSockets.WebSocket socket,
        OppoClientHolder oppoClientHolder,
        MediaPlayerEntityCommandMsgData<OppoCommandId> payload,
        string wsId,
        CancellationTokenWrapper cancellationTokenWrapper)
    {
        var success = true;
        switch (payload.MsgData.CommandId)
        {
            case OppoCommandId.On:
                var powerStateResponse = await HandlePowerOn(oppoClientHolder, cancellationTokenWrapper);

                if (powerStateResponse is not { Result: PowerState.Unknown })
                    // Run in background
                    _ = HandleEventUpdates(socket, wsId, oppoClientHolder, cancellationTokenWrapper);

                await SendPowerEvent(socket, payload, wsId, powerStateResponse.Result, cancellationTokenWrapper);

                break;
            case OppoCommandId.Off:
                await HandlePowerOff(oppoClientHolder, cancellationTokenWrapper);

                await SendPowerEvent(socket, payload, wsId, PowerState.Off, cancellationTokenWrapper);
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

                        await oppoClientHolder.Client.NumericInputAsync((ushort)digit, cancellationTokenWrapper.ApplicationStopping);
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
                await oppoClientHolder.Client.NumericInputAsync(0, cancellationTokenWrapper.ApplicationStopping);
                break;
            case OppoCommandId.Digit1:
                await oppoClientHolder.Client.NumericInputAsync(1, cancellationTokenWrapper.ApplicationStopping);
                break;
            case OppoCommandId.Digit2:
                await oppoClientHolder.Client.NumericInputAsync(2, cancellationTokenWrapper.ApplicationStopping);
                break;
            case OppoCommandId.Digit3:
                await oppoClientHolder.Client.NumericInputAsync(3, cancellationTokenWrapper.ApplicationStopping);
                break;
            case OppoCommandId.Digit4:
                await oppoClientHolder.Client.NumericInputAsync(4, cancellationTokenWrapper.ApplicationStopping);
                break;
            case OppoCommandId.Digit5:
                await oppoClientHolder.Client.NumericInputAsync(5, cancellationTokenWrapper.ApplicationStopping);
                break;
            case OppoCommandId.Digit6:
                await oppoClientHolder.Client.NumericInputAsync(6, cancellationTokenWrapper.ApplicationStopping);
                break;
            case OppoCommandId.Digit7:
                await oppoClientHolder.Client.NumericInputAsync(7, cancellationTokenWrapper.ApplicationStopping);
                break;
            case OppoCommandId.Digit8:
                await oppoClientHolder.Client.NumericInputAsync(8, cancellationTokenWrapper.ApplicationStopping);
                break;
            case OppoCommandId.Digit9:
                await oppoClientHolder.Client.NumericInputAsync(9, cancellationTokenWrapper.ApplicationStopping);
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
                ResponsePayloadHelpers.CreateCommonResponsePayload(payload),
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
                    }),
                wsId,
                cancellationTokenWrapper.ApplicationStopping);
        }
    }

    private async Task SendPowerEvent(System.Net.WebSockets.WebSocket socket,
        MediaPlayerEntityCommandMsgData<OppoCommandId> payload,
        string wsId,
        PowerState powerState,
        CancellationTokenWrapper cancellationTokenWrapper)
    {
        await SendAsync(socket,
            ResponsePayloadHelpers.CreateStateChangedResponsePayload(
                new MediaPlayerStateChangedEventMessageDataAttributes { State = powerState switch
                {
                    PowerState.On => State.On,
                    PowerState.Off => State.Off,
                    _ => State.Unknown
                } },
                payload.MsgData.EntityId,
                EntityType.MediaPlayer),
            wsId,
            cancellationTokenWrapper.ApplicationStopping);
        await SendAsync(socket,
            ResponsePayloadHelpers.CreateStateChangedResponsePayload(
                new RemoteStateChangedEventMessageDataAttributes { State = powerState switch
                {
                    PowerState.On => RemoteState.On,
                    PowerState.Off => RemoteState.Off,
                    _ => RemoteState.Unknown
                } },
                payload.MsgData.EntityId,
                EntityType.Remote),
            wsId,
            cancellationTokenWrapper.ApplicationStopping);
    }

    private static async Task HandlePowerOff(OppoClientHolder oppoClientHolder, CancellationTokenWrapper cancellationTokenWrapper)
    {
        await (cancellationTokenWrapper.GetCurrentBroadcastCancellationTokenSource()?.CancelAsync() ?? Task.CompletedTask);

        // Power commands can be flaky, so we try twice
        if ((await oppoClientHolder.Client.PowerOffAsync(cancellationTokenWrapper.ApplicationStopping)) is not { Result: PowerState.Off })
            await oppoClientHolder.Client.PowerOffAsync(cancellationTokenWrapper.ApplicationStopping);
    }

    private static async Task<OppoResult<PowerState>> HandlePowerOn(OppoClientHolder oppoClientHolder, CancellationTokenWrapper cancellationTokenWrapper)
    {
        cancellationTokenWrapper.EnsureNonCancelledBroadcastCancellationTokenSource();
        var powerStateResponse = await oppoClientHolder.Client.PowerOnAsync(cancellationTokenWrapper.ApplicationStopping);
        // Power commands can be flaky, so we try twice
        if (powerStateResponse is not { Result: PowerState.On })
            powerStateResponse = await oppoClientHolder.Client.PowerOnAsync(cancellationTokenWrapper.ApplicationStopping);
        return powerStateResponse;
    }

    private async Task HandleRemoteCommand(System.Net.WebSockets.WebSocket socket,
        OppoClientHolder oppoClientHolder,
        RemoteEntityCommandMsgData payload,
        string wsId,
        CancellationTokenWrapper cancellationTokenWrapper)
    {
        try
        {
            var success = true;
            switch (payload.MsgData.CommandId)
            {
                case "on":
                    var powerStateResult = await HandlePowerOn(oppoClientHolder, cancellationTokenWrapper);
                    await SendAsync(socket,
                        ResponsePayloadHelpers.CreateStateChangedResponsePayload(
                            new RemoteStateChangedEventMessageDataAttributes
                            {
                                State = powerStateResult.Result switch
                                {
                                    PowerState.On => RemoteState.On,
                                    PowerState.Off => RemoteState.Off,
                                    _ => RemoteState.Unknown
                                }
                            },
                            payload.MsgData.EntityId,
                            EntityType.Remote),
                        wsId,
                        cancellationTokenWrapper.ApplicationStopping);
                    await SendAsync(socket,
                        ResponsePayloadHelpers.CreateStateChangedResponsePayload(
                            new MediaPlayerStateChangedEventMessageDataAttributes { State = powerStateResult.Result switch
                            {
                                PowerState.On => State.On,
                                PowerState.Off => State.Off,
                                _ => State.Unknown
                            } },
                            payload.MsgData.EntityId,
                            EntityType.MediaPlayer),
                        wsId,
                        cancellationTokenWrapper.ApplicationStopping);
                    break;
                case "off":
                    await HandlePowerOff(oppoClientHolder, cancellationTokenWrapper);
                    await SendAsync(socket,
                        ResponsePayloadHelpers.CreateStateChangedResponsePayload(
                            new RemoteStateChangedEventMessageDataAttributes { State = RemoteState.Off },
                            payload.MsgData.EntityId,
                            EntityType.Remote),
                        wsId,
                        cancellationTokenWrapper.ApplicationStopping);
                    await SendAsync(socket,
                        ResponsePayloadHelpers.CreateStateChangedResponsePayload(
                            new MediaPlayerStateChangedEventMessageDataAttributes { State = State.Off },
                            payload.MsgData.EntityId,
                            EntityType.MediaPlayer),
                        wsId,
                        cancellationTokenWrapper.ApplicationStopping);
                    break;
                case "toggle":
                    await oppoClientHolder.Client.PowerToggleAsync(cancellationTokenWrapper.ApplicationStopping);
                    break;
                case "send_cmd":
                    success = await HandleSendCommand(payload, cancellationTokenWrapper, oppoClientHolder);
                    break;
                case "send_cmd_sequence":
                    success = await HandleSendCommandSequence(payload, cancellationTokenWrapper, oppoClientHolder);
                    break;
                default:
                    success = false;
                    break;
            }

            if (success)
            {
                await SendAsync(socket,
                    ResponsePayloadHelpers.CreateCommonResponsePayload(payload),
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
                        }),
                    wsId,
                    cancellationTokenWrapper.ApplicationStopping);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[{WSId}] WS: Error while handling entity command {EntityCommand}", wsId, payload.MsgData);
            await SendAsync(socket,
                ResponsePayloadHelpers.CreateValidationErrorResponsePayload(payload,
                    new ValidationError
                    {
                        Code = "ERROR",
                        Message = "Error while handling command"
                    }),
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

    private static async Task<bool> HandleSendCommand(RemoteEntityCommandMsgData payload,
        CancellationTokenWrapper cancellationTokenWrapper,
        OppoClientHolder oppoClientHolder)
    {
        var command = payload.MsgData.Params?.Command;
        if (string.IsNullOrEmpty(command))
            return false;

        var delay = payload.MsgData.Params?.Delay ?? 0;
        if (payload.MsgData.Params?.Repeat.HasValue is true)
        {
            for (var i = 0; i < payload.MsgData.Params.Repeat.Value; i++)
            {
                await ExecuteCommand(command, oppoClientHolder.Client, cancellationTokenWrapper.ApplicationStopping);
                if (delay> 0)
                    await Task.Delay(TimeSpan.FromMilliseconds(delay), cancellationTokenWrapper.ApplicationStopping);
            }
        }
        else
        {
            await ExecuteCommand(command, oppoClientHolder.Client, cancellationTokenWrapper.ApplicationStopping);
        }

        return true;
    }

    private static async Task<bool> HandleSendCommandSequence(RemoteEntityCommandMsgData payload,
        CancellationTokenWrapper cancellationTokenWrapper,
        OppoClientHolder oppoClientHolder)
    {
        if (payload.MsgData.Params is not { Sequence: { Length: > 0 } sequence })
            return false;

        var delay = payload.MsgData.Params?.Delay ?? 0;
        var shouldRepeat = payload.MsgData.Params?.Repeat.HasValue is true;
        foreach (var command in sequence.Where(static x => !string.IsNullOrEmpty(x)))
        {
            if (shouldRepeat)
            {
                for (var i = 0; i < payload.MsgData.Params!.Repeat!.Value; i++)
                {
                    await ExecuteCommand(command, oppoClientHolder.Client, cancellationTokenWrapper.ApplicationStopping);
                    if (delay> 0)
                        await Task.Delay(TimeSpan.FromMilliseconds(delay), cancellationTokenWrapper.ApplicationStopping);
                }
            }
            else
            {
                await ExecuteCommand(command, oppoClientHolder.Client, cancellationTokenWrapper.ApplicationStopping);
                await Task.Delay(TimeSpan.FromMilliseconds(delay), cancellationTokenWrapper.ApplicationStopping);
            }
        }

        return true;
    }

    private static async ValueTask<bool> ExecuteCommand(string command, IOppoClient client, CancellationToken cancellationToken)
    {
        return command switch
        {
            _ when command.Equals(RemoteCommandIdConstants.On, StringComparison.OrdinalIgnoreCase) => await client.PowerOnAsync(cancellationToken),
            _ when command.Equals(RemoteCommandIdConstants.Off, StringComparison.OrdinalIgnoreCase) => await client.PowerOffAsync(cancellationToken),
            _ when command.Equals(RemoteCommandIdConstants.Toggle, StringComparison.OrdinalIgnoreCase) => await client.PowerToggleAsync(cancellationToken),
            _ when command.Equals(MediaPlayerCommandIdConstants.PlayPause, StringComparison.OrdinalIgnoreCase) => await client.PauseAsync(cancellationToken),
            _ when command.Equals(RemoteCommandIdConstants.Previous, StringComparison.OrdinalIgnoreCase) => await client.PreviousAsync(cancellationToken),
            _ when command.Equals(RemoteCommandIdConstants.Next, StringComparison.OrdinalIgnoreCase) => await client.NextAsync(cancellationToken),
            _ when command.Equals(MediaPlayerCommandIdConstants.FastForward, StringComparison.OrdinalIgnoreCase) => await client.ForwardAsync(cancellationToken),
            _ when command.Equals(MediaPlayerCommandIdConstants.Rewind, StringComparison.OrdinalIgnoreCase) => await client.ReverseAsync(cancellationToken),
            _ when command.Equals(RemoteCommandIdConstants.VolumeUp, StringComparison.OrdinalIgnoreCase) => await client.VolumeUpAsync(cancellationToken),
            _ when command.Equals(RemoteCommandIdConstants.VolumeDown, StringComparison.OrdinalIgnoreCase) => await client.VolumeDownAsync(cancellationToken),
            _ when command.Equals(RemoteCommandIdConstants.MuteToggle, StringComparison.OrdinalIgnoreCase) => await client.MuteToggleAsync(cancellationToken),
            _ when command.Equals(MediaPlayerCommandIdConstants.Repeat, StringComparison.OrdinalIgnoreCase) => await client.RepeatAsync(cancellationToken),
            _ when command.Equals(RemoteCommandIdConstants.ChannelUp, StringComparison.OrdinalIgnoreCase) => await client.PageUpAsync(cancellationToken),
            _ when command.Equals(RemoteCommandIdConstants.ChannelDown, StringComparison.OrdinalIgnoreCase) => await client.PageDownAsync(cancellationToken),
            _ when command.Equals(RemoteCommandIdConstants.CursorUp, StringComparison.OrdinalIgnoreCase) => await client.UpArrowAsync(cancellationToken),
            _ when command.Equals(RemoteCommandIdConstants.CursorDown, StringComparison.OrdinalIgnoreCase) => await client.DownArrowAsync(cancellationToken),
            _ when command.Equals(RemoteCommandIdConstants.CursorLeft, StringComparison.OrdinalIgnoreCase) => await client.LeftArrowAsync(cancellationToken),
            _ when command.Equals(RemoteCommandIdConstants.CursorRight, StringComparison.OrdinalIgnoreCase) => await client.RightArrowAsync(cancellationToken),
            _ when command.Equals(RemoteCommandIdConstants.CursorEnter, StringComparison.OrdinalIgnoreCase) => await client.EnterAsync(cancellationToken),
            _ when command.Equals(MediaPlayerCommandIdConstants.Digit0, StringComparison.OrdinalIgnoreCase) => await client.NumericInputAsync(0, cancellationToken),
            _ when command.Equals(MediaPlayerCommandIdConstants.Digit1, StringComparison.OrdinalIgnoreCase) => await client.NumericInputAsync(1, cancellationToken),
            _ when command.Equals(MediaPlayerCommandIdConstants.Digit2, StringComparison.OrdinalIgnoreCase) => await client.NumericInputAsync(2, cancellationToken),
            _ when command.Equals(MediaPlayerCommandIdConstants.Digit3, StringComparison.OrdinalIgnoreCase) => await client.NumericInputAsync(3, cancellationToken),
            _ when command.Equals(MediaPlayerCommandIdConstants.Digit4, StringComparison.OrdinalIgnoreCase) => await client.NumericInputAsync(4, cancellationToken),
            _ when command.Equals(MediaPlayerCommandIdConstants.Digit5, StringComparison.OrdinalIgnoreCase) => await client.NumericInputAsync(5, cancellationToken),
            _ when command.Equals(MediaPlayerCommandIdConstants.Digit6, StringComparison.OrdinalIgnoreCase) => await client.NumericInputAsync(6, cancellationToken),
            _ when command.Equals(MediaPlayerCommandIdConstants.Digit7, StringComparison.OrdinalIgnoreCase) => await client.NumericInputAsync(7, cancellationToken),
            _ when command.Equals(MediaPlayerCommandIdConstants.Digit8, StringComparison.OrdinalIgnoreCase) => await client.NumericInputAsync(8, cancellationToken),
            _ when command.Equals(MediaPlayerCommandIdConstants.Digit9, StringComparison.OrdinalIgnoreCase) => await client.NumericInputAsync(9, cancellationToken),
            _ when command.Equals(RemoteCommandIdConstants.FunctionRed, StringComparison.OrdinalIgnoreCase) => await client.RedAsync(cancellationToken),
            _ when command.Equals(RemoteCommandIdConstants.FunctionGreen, StringComparison.OrdinalIgnoreCase) => await client.GreenAsync(cancellationToken),
            _ when command.Equals(RemoteCommandIdConstants.FunctionYellow, StringComparison.OrdinalIgnoreCase) => await client.YellowAsync(cancellationToken),
            _ when command.Equals(RemoteCommandIdConstants.FunctionBlue, StringComparison.OrdinalIgnoreCase) => await client.BlueAsync(cancellationToken),
            _ when command.Equals(RemoteCommandIdConstants.Home, StringComparison.OrdinalIgnoreCase) => await client.HomeAsync(cancellationToken),
            _ when command.Equals(MediaPlayerCommandIdConstants.ContextMenu, StringComparison.OrdinalIgnoreCase) => await client.TopMenuAsync(cancellationToken),
            _ when command.Equals(MediaPlayerCommandIdConstants.Info, StringComparison.OrdinalIgnoreCase) => await client.InfoToggleAsync(cancellationToken),
            _ when command.Equals(RemoteCommandIdConstants.Back, StringComparison.OrdinalIgnoreCase) => await client.ReturnAsync(cancellationToken),
            _ when command.Equals(MediaPlayerCommandIdConstants.Eject, StringComparison.OrdinalIgnoreCase) => await client.EjectToggleAsync(cancellationToken),
            _ when command.Equals(MediaPlayerCommandIdConstants.Subtitle, StringComparison.OrdinalIgnoreCase) => await client.SubtitleAsync(cancellationToken),
            _ when command.Equals(MediaPlayerCommandIdConstants.Settings, StringComparison.OrdinalIgnoreCase) => await client.SetupAsync(cancellationToken),
            _ when command.Equals(RemoteCommandIdConstants.Power, StringComparison.OrdinalIgnoreCase) => await client.PowerToggleAsync(cancellationToken),
            _ when command.Equals(EntitySettingsConstants.Dimmer, StringComparison.OrdinalIgnoreCase) => await client.DimmerAsync(cancellationToken),
            _ when command.Equals(EntitySettingsConstants.PureAudioToggle, StringComparison.OrdinalIgnoreCase) => await client.PureAudioToggleAsync(cancellationToken),
            _ when command.Equals(EntitySettingsConstants.Clear, StringComparison.OrdinalIgnoreCase) => await client.ClearAsync(cancellationToken),
            _ when command.Equals(EntitySettingsConstants.TopMenu, StringComparison.OrdinalIgnoreCase) => await client.TopMenuAsync(cancellationToken),
            _ when command.Equals(EntitySettingsConstants.PopUpMenu, StringComparison.OrdinalIgnoreCase) => await client.PopUpMenuAsync(cancellationToken),
            _ when command.Equals(EntitySettingsConstants.Pause, StringComparison.OrdinalIgnoreCase) => await client.PauseAsync(cancellationToken),
            _ when command.Equals(EntitySettingsConstants.Play, StringComparison.OrdinalIgnoreCase) => await client.PlayAsync(cancellationToken),
            _ when command.Equals(EntitySettingsConstants.Angle, StringComparison.OrdinalIgnoreCase) => await client.AngleAsync(cancellationToken),
            _ when command.Equals(EntitySettingsConstants.Zoom, StringComparison.OrdinalIgnoreCase) => await client.ZoomAsync(cancellationToken),
            _ when command.Equals(EntitySettingsConstants.SecondaryAudioProgram, StringComparison.OrdinalIgnoreCase) => await client.SecondaryAudioProgramAsync(cancellationToken),
            _ when command.Equals(EntitySettingsConstants.AbReplay, StringComparison.OrdinalIgnoreCase) => await client.ABReplayAsync(cancellationToken),
            _ when command.Equals(EntitySettingsConstants.PictureInPicture, StringComparison.OrdinalIgnoreCase) => await client.PictureInPictureAsync(cancellationToken),
            _ when command.Equals(EntitySettingsConstants.Resolution, StringComparison.OrdinalIgnoreCase) => await client.ResolutionAsync(cancellationToken),
            _ when command.Equals(EntitySettingsConstants.SubtitleHold, StringComparison.OrdinalIgnoreCase) => await client.SubtitleHoldAsync(cancellationToken),
            _ when command.Equals(EntitySettingsConstants.Option, StringComparison.OrdinalIgnoreCase) => await client.OptionAsync(cancellationToken),
            _ when command.Equals(EntitySettingsConstants.ThreeD, StringComparison.OrdinalIgnoreCase) => await client.ThreeDAsync(cancellationToken),
            _ when command.Equals(EntitySettingsConstants.PictureAdjustment, StringComparison.OrdinalIgnoreCase) => await client.PictureAdjustmentAsync(cancellationToken),
            _ when command.Equals(EntitySettingsConstants.Hdr, StringComparison.OrdinalIgnoreCase) => await client.HDRAsync(cancellationToken),
            _ when command.Equals(EntitySettingsConstants.InfoHold, StringComparison.OrdinalIgnoreCase) => await client.InfoHoldAsync(cancellationToken),
            _ when command.Equals(EntitySettingsConstants.ResolutionHold, StringComparison.OrdinalIgnoreCase) => await client.ResolutionHoldAsync(cancellationToken),
            _ when command.Equals(EntitySettingsConstants.AvSync, StringComparison.OrdinalIgnoreCase) => await client.AVSyncAsync(cancellationToken),
            _ when command.Equals(EntitySettingsConstants.GaplessPlay, StringComparison.OrdinalIgnoreCase) => await client.GaplessPlayAsync(cancellationToken),

            _ => false
        };
    }
}