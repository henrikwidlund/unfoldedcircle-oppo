using Oppo;

using UnfoldedCircle.Models.Sync;
using UnfoldedCircle.OppoBluRay.Logging;
using UnfoldedCircle.OppoBluRay.OppoEntity;
using UnfoldedCircle.Server.Extensions;
using UnfoldedCircle.Server.WebSocket;

namespace UnfoldedCircle.OppoBluRay.WebSocket;

public partial class OppoWebSocketHandler
{
    protected override async ValueTask<EntityCommandResult> OnMediaPlayerCommandAsync(System.Net.WebSockets.WebSocket socket,
        MediaPlayerEntityCommandMsgData<OppoCommandId> payload,
        string wsId,
        CancellationTokenWrapper cancellationTokenWrapper,
        CancellationToken commandCancellationToken)
    {
        var oppoClientHolder = await TryGetOppoClientHolderAsync(wsId, payload.MsgData.EntityId, IdentifierType.EntityId, commandCancellationToken);
        if (oppoClientHolder is null)
        {
            _logger.CouldNotFindOppoClientForEntityIdMemory(wsId, payload.MsgData.EntityId.AsMemory().GetBaseIdentifier());
            return EntityCommandResult.Failure;
        }

        OppoResult<PowerState>? powerState = payload.MsgData.CommandId switch
        {
            OppoCommandId.On => await HandlePowerOnAsync(oppoClientHolder, cancellationTokenWrapper, commandCancellationToken),
            OppoCommandId.Off => await HandlePowerOffAsync(oppoClientHolder, commandCancellationToken),
            OppoCommandId.Toggle => await oppoClientHolder.Client.PowerToggleAsync(commandCancellationToken),
            _ => null
        };

        if (powerState is not null)
        {
            return powerState.Value.Result switch
            {
                PowerState.On => EntityCommandResult.PowerOn,
                PowerState.Off => EntityCommandResult.PowerOff,
                _ => EntityCommandResult.Failure
            };
        }

        var success = true;
        switch (payload.MsgData.CommandId)
        {
            case OppoCommandId.PlayPause:
                await oppoClientHolder.Client.PauseAsync(commandCancellationToken);
                break;
            case OppoCommandId.Stop:
                await oppoClientHolder.Client.StopAsync(commandCancellationToken);
                break;
            case OppoCommandId.Previous:
                await oppoClientHolder.Client.PreviousAsync(commandCancellationToken);
                break;
            case OppoCommandId.Next:
                await oppoClientHolder.Client.NextAsync(commandCancellationToken);
                break;
            case OppoCommandId.FastForward:
                await oppoClientHolder.Client.ForwardAsync(commandCancellationToken);
                break;
            case OppoCommandId.Rewind:
                await oppoClientHolder.Client.ReverseAsync(commandCancellationToken);
                break;
            case OppoCommandId.Seek:
                if (payload.MsgData.Params is { MediaPosition: not null })
                {
                    var digits = GetDigits(payload.MsgData.Params.MediaPosition.Value);
                    await oppoClientHolder.Client.GoToAsync(commandCancellationToken);
                    foreach (uint digit in digits)
                    {
                        if (digit > 9)
                        {
                            await oppoClientHolder.Client.ClearAsync(commandCancellationToken);
                            await oppoClientHolder.Client.EnterAsync(commandCancellationToken);
                            return EntityCommandResult.Other;
                        }

                        await oppoClientHolder.Client.NumericInputAsync((ushort)digit, commandCancellationToken);
                    }

                    await oppoClientHolder.Client.EnterAsync(commandCancellationToken);
                }
                break;
            case OppoCommandId.VolumeUp:
                await oppoClientHolder.Client.VolumeUpAsync(commandCancellationToken);
                break;
            case OppoCommandId.VolumeDown:
                await oppoClientHolder.Client.VolumeDownAsync(commandCancellationToken);
                break;
            case OppoCommandId.MuteToggle:
                await oppoClientHolder.Client.MuteToggleAsync(commandCancellationToken);
                break;
            case OppoCommandId.Repeat:
                if (payload.MsgData.Params is { Repeat: not null })
                    await oppoClientHolder.Client.SetRepeatAsync(payload.MsgData.Params.Repeat switch
                    {
                        Models.Shared.RepeatMode.Off => RepeatMode.Off,
                        Models.Shared.RepeatMode.All => RepeatMode.All,
                        Models.Shared.RepeatMode.One => RepeatMode.Title,
                        _ => RepeatMode.Off
                    }, commandCancellationToken);
                else
                    await oppoClientHolder.Client.RepeatAsync(commandCancellationToken);

                break;
            case OppoCommandId.ChannelUp:
                await oppoClientHolder.Client.PageUpAsync(commandCancellationToken);
                break;
            case OppoCommandId.ChannelDown:
                await oppoClientHolder.Client.PageDownAsync(commandCancellationToken);
                break;
            case OppoCommandId.CursorUp:
                await oppoClientHolder.Client.UpArrowAsync(commandCancellationToken);
                break;
            case OppoCommandId.CursorDown:
                await oppoClientHolder.Client.DownArrowAsync(commandCancellationToken);
                break;
            case OppoCommandId.CursorLeft:
                await oppoClientHolder.Client.LeftArrowAsync(commandCancellationToken);
                break;
            case OppoCommandId.CursorRight:
                await oppoClientHolder.Client.RightArrowAsync(commandCancellationToken);
                break;
            case OppoCommandId.CursorEnter:
                await oppoClientHolder.Client.EnterAsync(commandCancellationToken);
                break;
            case OppoCommandId.Digit0:
                await oppoClientHolder.Client.NumericInputAsync(0, commandCancellationToken);
                break;
            case OppoCommandId.Digit1:
                await oppoClientHolder.Client.NumericInputAsync(1, commandCancellationToken);
                break;
            case OppoCommandId.Digit2:
                await oppoClientHolder.Client.NumericInputAsync(2, commandCancellationToken);
                break;
            case OppoCommandId.Digit3:
                await oppoClientHolder.Client.NumericInputAsync(3, commandCancellationToken);
                break;
            case OppoCommandId.Digit4:
                await oppoClientHolder.Client.NumericInputAsync(4, commandCancellationToken);
                break;
            case OppoCommandId.Digit5:
                await oppoClientHolder.Client.NumericInputAsync(5, commandCancellationToken);
                break;
            case OppoCommandId.Digit6:
                await oppoClientHolder.Client.NumericInputAsync(6, commandCancellationToken);
                break;
            case OppoCommandId.Digit7:
                await oppoClientHolder.Client.NumericInputAsync(7, commandCancellationToken);
                break;
            case OppoCommandId.Digit8:
                await oppoClientHolder.Client.NumericInputAsync(8, commandCancellationToken);
                break;
            case OppoCommandId.Digit9:
                await oppoClientHolder.Client.NumericInputAsync(9, commandCancellationToken);
                break;
            case OppoCommandId.FunctionRed:
                await oppoClientHolder.Client.RedAsync(commandCancellationToken);
                break;
            case OppoCommandId.FunctionGreen:
                await oppoClientHolder.Client.GreenAsync(commandCancellationToken);
                break;
            case OppoCommandId.FunctionYellow:
                await oppoClientHolder.Client.YellowAsync(commandCancellationToken);
                break;
            case OppoCommandId.FunctionBlue:
                await oppoClientHolder.Client.BlueAsync(commandCancellationToken);
                break;
            case OppoCommandId.Home:
                await oppoClientHolder.Client.HomeAsync(commandCancellationToken);
                break;
            case OppoCommandId.ContextMenu:
                await oppoClientHolder.Client.TopMenuAsync(commandCancellationToken);
                break;
            case OppoCommandId.Info:
                await oppoClientHolder.Client.InfoToggleAsync(commandCancellationToken);
                break;
            case OppoCommandId.Back:
                await oppoClientHolder.Client.ReturnAsync(commandCancellationToken);
                break;
            case OppoCommandId.SelectSource:
                if (payload.MsgData.Params is { Source: not null } && OppoEntitySettings.SourceMap.TryGetValue(payload.MsgData.Params.Source, out var source))
                {
                    // Sending input source is only allowed if the unit is on - avoid locking up the driver by only sending it when the unit is ready
                    var currentPowerState = await oppoClientHolder.Client.QueryPowerStatusAsync(commandCancellationToken);
                    if (currentPowerState is { Result: PowerState.On })
                        await oppoClientHolder.Client.SetInputSourceAsync(source, commandCancellationToken);
                }
                else
                    await oppoClientHolder.Client.InputAsync(commandCancellationToken);
                break;
            case OppoCommandId.PureAudioToggle:
                await oppoClientHolder.Client.PureAudioToggleAsync(commandCancellationToken);
                break;
            case OppoCommandId.OpenClose:
                await oppoClientHolder.Client.EjectToggleAsync(commandCancellationToken);
                break;
            case OppoCommandId.AudioTrack:
                await oppoClientHolder.Client.AudioAsync(commandCancellationToken);
                break;
            case OppoCommandId.Subtitle:
                await oppoClientHolder.Client.SubtitleAsync(commandCancellationToken);
                break;
            case OppoCommandId.Settings:
                await oppoClientHolder.Client.SetupAsync(commandCancellationToken);
                break;
            case OppoCommandId.Dimmer:
                await oppoClientHolder.Client.DimmerAsync(commandCancellationToken);
                break;
            case OppoCommandId.Clear:
                await oppoClientHolder.Client.ClearAsync(commandCancellationToken);
                break;
            case OppoCommandId.PopUpMenu:
                await oppoClientHolder.Client.PopUpMenuAsync(commandCancellationToken);
                break;
            case OppoCommandId.Pause:
                await oppoClientHolder.Client.PauseAsync(commandCancellationToken);
                break;
            case OppoCommandId.Play:
                await oppoClientHolder.Client.PlayAsync(commandCancellationToken);
                break;
            case OppoCommandId.Angle:
                await oppoClientHolder.Client.AngleAsync(commandCancellationToken);
                break;
            case OppoCommandId.Zoom:
                await oppoClientHolder.Client.ZoomAsync(commandCancellationToken);
                break;
            case OppoCommandId.SecondaryAudioProgram:
                await oppoClientHolder.Client.SecondaryAudioProgramAsync(commandCancellationToken);
                break;
            case OppoCommandId.AbReplay:
                await oppoClientHolder.Client.ABReplayAsync(commandCancellationToken);
                break;
            case OppoCommandId.PictureInPicture:
                await oppoClientHolder.Client.PictureInPictureAsync(commandCancellationToken);
                break;
            case OppoCommandId.Resolution:
                await oppoClientHolder.Client.ResolutionAsync(commandCancellationToken);
                break;
            case OppoCommandId.SubtitleHold:
                await oppoClientHolder.Client.SubtitleHoldAsync(commandCancellationToken);
                break;
            case OppoCommandId.Option:
                await oppoClientHolder.Client.OptionAsync(commandCancellationToken);
                break;
            case OppoCommandId.ThreeD:
                await oppoClientHolder.Client.ThreeDAsync(commandCancellationToken);
                break;
            case OppoCommandId.PictureAdjustment:
                await oppoClientHolder.Client.PictureAdjustmentAsync(commandCancellationToken);
                break;
            case OppoCommandId.Hdr:
                await oppoClientHolder.Client.HDRAsync(commandCancellationToken);
                break;
            case OppoCommandId.InfoHold:
                await oppoClientHolder.Client.InfoHoldAsync(commandCancellationToken);
                break;
            case OppoCommandId.ResolutionHold:
                await oppoClientHolder.Client.ResolutionHoldAsync(commandCancellationToken);
                break;
            case OppoCommandId.AvSync:
                await oppoClientHolder.Client.AVSyncAsync(commandCancellationToken);
                break;
            case OppoCommandId.GaplessPlay:
                await oppoClientHolder.Client.GaplessPlayAsync(commandCancellationToken);
                break;

            case OppoCommandId.Shuffle:
                if (payload.MsgData.Params is { Shuffle: not null })
                    await oppoClientHolder.Client.SetRepeatAsync(payload.MsgData.Params.Shuffle.Value ? RepeatMode.Shuffle : RepeatMode.Off, commandCancellationToken);
                break;

            case OppoCommandId.Volume:
                if (payload.MsgData.Params is { Volume: not null })
                    await oppoClientHolder.Client.SetVolumeAsync(payload.MsgData.Params.Volume.Value, commandCancellationToken);
                break;

            // unsupported default commands
            default:
                success = false;
                break;
        }

        return success ? EntityCommandResult.Other : EntityCommandResult.Failure;
    }

    protected override async ValueTask<EntityCommandResult> OnRemoteCommandAsync(System.Net.WebSockets.WebSocket socket,
        RemoteEntityCommandMsgData payload,
        string command,
        string wsId,
        CancellationTokenWrapper cancellationTokenWrapper,
        CancellationToken commandCancellationToken)
    {
        if (await TryGetOppoClientHolderAsync(wsId, payload.MsgData.EntityId, IdentifierType.EntityId, commandCancellationToken) is not { } oppoClientHolder)
        {
            _logger.CouldNotFindOppoClientForEntityIdMemory(wsId, payload.MsgData.EntityId.AsMemory().GetBaseIdentifier());
            return EntityCommandResult.Failure;
        }

        var client = oppoClientHolder.Client;
        OppoResult<PowerState>? powerState = command switch
        {
            _ when command.Equals(RemoteButtonConstants.On, StringComparison.OrdinalIgnoreCase) => await HandlePowerOnAsync(oppoClientHolder, cancellationTokenWrapper, commandCancellationToken),
            _ when command.Equals(RemoteButtonConstants.Off, StringComparison.OrdinalIgnoreCase) => await HandlePowerOffAsync(oppoClientHolder, commandCancellationToken),
            _ when command.Equals(RemoteButtonConstants.Toggle, StringComparison.OrdinalIgnoreCase) => await client.PowerToggleAsync(commandCancellationToken),
            _ when command.Equals(RemoteButtonConstants.Power, StringComparison.OrdinalIgnoreCase) => await client.PowerToggleAsync(commandCancellationToken),
            _ => null
        };

        if (powerState is not null)
        {
            return powerState.Value.Result switch
            {
                PowerState.On => EntityCommandResult.PowerOn,
                PowerState.Off => EntityCommandResult.PowerOff,
                _ => EntityCommandResult.Failure
            };
        }

        var result = command switch
        {
            _ when command.Equals(MediaPlayerCommandIdConstants.PlayPause, StringComparison.OrdinalIgnoreCase) => await client.PauseAsync(commandCancellationToken),
            _ when command.Equals(RemoteButtonConstants.Stop, StringComparison.OrdinalIgnoreCase) => await client.StopAsync(commandCancellationToken),
            _ when command.Equals(RemoteButtonConstants.Menu, StringComparison.OrdinalIgnoreCase) => await client.TopMenuAsync(commandCancellationToken),
            _ when command.Equals(RemoteButtonConstants.Previous, StringComparison.OrdinalIgnoreCase) => await client.PreviousAsync(commandCancellationToken),
            _ when command.Equals(RemoteButtonConstants.Next, StringComparison.OrdinalIgnoreCase) => await client.NextAsync(commandCancellationToken),
            _ when command.Equals(MediaPlayerCommandIdConstants.FastForward, StringComparison.OrdinalIgnoreCase) => (bool)await client.ForwardAsync(commandCancellationToken),
            _ when command.Equals(MediaPlayerCommandIdConstants.Rewind, StringComparison.OrdinalIgnoreCase) => (bool)await client.ReverseAsync(commandCancellationToken),
            _ when command.Equals(RemoteButtonConstants.VolumeUp, StringComparison.OrdinalIgnoreCase) => (bool)await client.VolumeUpAsync(commandCancellationToken),
            _ when command.Equals(RemoteButtonConstants.VolumeDown, StringComparison.OrdinalIgnoreCase) => (bool)await client.VolumeDownAsync(commandCancellationToken),
            _ when command.Equals(RemoteButtonConstants.Mute, StringComparison.OrdinalIgnoreCase) => (bool)await client.MuteToggleAsync(commandCancellationToken),
            _ when command.Equals(MediaPlayerCommandIdConstants.Repeat, StringComparison.OrdinalIgnoreCase) => (bool)await client.RepeatAsync(commandCancellationToken),
            _ when command.Equals(RemoteButtonConstants.ChannelUp, StringComparison.OrdinalIgnoreCase) => await client.PageUpAsync(commandCancellationToken),
            _ when command.Equals(RemoteButtonConstants.ChannelDown, StringComparison.OrdinalIgnoreCase) => await client.PageDownAsync(commandCancellationToken),
            _ when command.Equals(RemoteButtonConstants.DpadUp, StringComparison.OrdinalIgnoreCase) => await client.UpArrowAsync(commandCancellationToken),
            _ when command.Equals(RemoteButtonConstants.DpadDown, StringComparison.OrdinalIgnoreCase) => await client.DownArrowAsync(commandCancellationToken),
            _ when command.Equals(RemoteButtonConstants.DpadLeft, StringComparison.OrdinalIgnoreCase) => await client.LeftArrowAsync(commandCancellationToken),
            _ when command.Equals(RemoteButtonConstants.DpadRight, StringComparison.OrdinalIgnoreCase) => await client.RightArrowAsync(commandCancellationToken),
            _ when command.Equals(RemoteButtonConstants.DpadMiddle, StringComparison.OrdinalIgnoreCase) => await client.EnterAsync(commandCancellationToken),
            _ when command.Equals(MediaPlayerCommandIdConstants.Digit0, StringComparison.OrdinalIgnoreCase) => await client.NumericInputAsync(0, commandCancellationToken),
            _ when command.Equals(MediaPlayerCommandIdConstants.Digit1, StringComparison.OrdinalIgnoreCase) => await client.NumericInputAsync(1, commandCancellationToken),
            _ when command.Equals(MediaPlayerCommandIdConstants.Digit2, StringComparison.OrdinalIgnoreCase) => await client.NumericInputAsync(2, commandCancellationToken),
            _ when command.Equals(MediaPlayerCommandIdConstants.Digit3, StringComparison.OrdinalIgnoreCase) => await client.NumericInputAsync(3, commandCancellationToken),
            _ when command.Equals(MediaPlayerCommandIdConstants.Digit4, StringComparison.OrdinalIgnoreCase) => await client.NumericInputAsync(4, commandCancellationToken),
            _ when command.Equals(MediaPlayerCommandIdConstants.Digit5, StringComparison.OrdinalIgnoreCase) => await client.NumericInputAsync(5, commandCancellationToken),
            _ when command.Equals(MediaPlayerCommandIdConstants.Digit6, StringComparison.OrdinalIgnoreCase) => await client.NumericInputAsync(6, commandCancellationToken),
            _ when command.Equals(MediaPlayerCommandIdConstants.Digit7, StringComparison.OrdinalIgnoreCase) => await client.NumericInputAsync(7, commandCancellationToken),
            _ when command.Equals(MediaPlayerCommandIdConstants.Digit8, StringComparison.OrdinalIgnoreCase) => await client.NumericInputAsync(8, commandCancellationToken),
            _ when command.Equals(MediaPlayerCommandIdConstants.Digit9, StringComparison.OrdinalIgnoreCase) => await client.NumericInputAsync(9, commandCancellationToken),
            _ when command.Equals(RemoteButtonConstants.Red, StringComparison.OrdinalIgnoreCase) => await client.RedAsync(commandCancellationToken),
            _ when command.Equals(RemoteButtonConstants.Green, StringComparison.OrdinalIgnoreCase) => await client.GreenAsync(commandCancellationToken),
            _ when command.Equals(RemoteButtonConstants.Yellow, StringComparison.OrdinalIgnoreCase) => await client.YellowAsync(commandCancellationToken),
            _ when command.Equals(RemoteButtonConstants.Blue, StringComparison.OrdinalIgnoreCase) => await client.BlueAsync(commandCancellationToken),
            _ when command.Equals(RemoteButtonConstants.Home, StringComparison.OrdinalIgnoreCase) => await client.HomeAsync(commandCancellationToken),
            _ when command.Equals(MediaPlayerCommandIdConstants.ContextMenu, StringComparison.OrdinalIgnoreCase) => await client.TopMenuAsync(commandCancellationToken),
            _ when command.Equals(MediaPlayerCommandIdConstants.Info, StringComparison.OrdinalIgnoreCase) => await client.InfoToggleAsync(commandCancellationToken),
            _ when command.Equals(RemoteButtonConstants.Back, StringComparison.OrdinalIgnoreCase) => await client.ReturnAsync(commandCancellationToken),
            _ when command.Equals(MediaPlayerCommandIdConstants.Eject, StringComparison.OrdinalIgnoreCase) => (bool)await client.EjectToggleAsync(commandCancellationToken),
            _ when command.Equals(MediaPlayerCommandIdConstants.Subtitle, StringComparison.OrdinalIgnoreCase) => await client.SubtitleAsync(commandCancellationToken),
            _ when command.Equals(MediaPlayerCommandIdConstants.Settings, StringComparison.OrdinalIgnoreCase) => await client.SetupAsync(commandCancellationToken),
            _ when command.Equals(EntitySettingsConstants.Dimmer, StringComparison.OrdinalIgnoreCase) => (bool)await client.DimmerAsync(commandCancellationToken),
            _ when command.Equals(EntitySettingsConstants.PureAudioToggle, StringComparison.OrdinalIgnoreCase) => (bool)await client.PureAudioToggleAsync(commandCancellationToken),
            _ when command.Equals(EntitySettingsConstants.Clear, StringComparison.OrdinalIgnoreCase) => await client.ClearAsync(commandCancellationToken),
            _ when command.Equals(EntitySettingsConstants.TopMenu, StringComparison.OrdinalIgnoreCase) => await client.TopMenuAsync(commandCancellationToken),
            _ when command.Equals(EntitySettingsConstants.PopUpMenu, StringComparison.OrdinalIgnoreCase) => await client.PopUpMenuAsync(commandCancellationToken),
            _ when command.Equals(EntitySettingsConstants.Pause, StringComparison.OrdinalIgnoreCase) => await client.PauseAsync(commandCancellationToken),
            _ when command.Equals(EntitySettingsConstants.Play, StringComparison.OrdinalIgnoreCase) => await client.PlayAsync(commandCancellationToken),
            _ when command.Equals(EntitySettingsConstants.Angle, StringComparison.OrdinalIgnoreCase) => (bool)await client.AngleAsync(commandCancellationToken),
            _ when command.Equals(EntitySettingsConstants.Zoom, StringComparison.OrdinalIgnoreCase) => (bool)await client.ZoomAsync(commandCancellationToken),
            _ when command.Equals(EntitySettingsConstants.SecondaryAudioProgram, StringComparison.OrdinalIgnoreCase) => (bool)await client.SecondaryAudioProgramAsync(commandCancellationToken),
            _ when command.Equals(EntitySettingsConstants.AbReplay, StringComparison.OrdinalIgnoreCase) => (bool)await client.ABReplayAsync(commandCancellationToken),
            _ when command.Equals(EntitySettingsConstants.PictureInPicture, StringComparison.OrdinalIgnoreCase) => (bool)await client.PictureInPictureAsync(commandCancellationToken),
            _ when command.Equals(EntitySettingsConstants.Resolution, StringComparison.OrdinalIgnoreCase) => await client.ResolutionAsync(commandCancellationToken),
            _ when command.Equals(EntitySettingsConstants.SubtitleHold, StringComparison.OrdinalIgnoreCase) => await client.SubtitleHoldAsync(commandCancellationToken),
            _ when command.Equals(EntitySettingsConstants.Option, StringComparison.OrdinalIgnoreCase) => await client.OptionAsync(commandCancellationToken),
            _ when command.Equals(EntitySettingsConstants.ThreeD, StringComparison.OrdinalIgnoreCase) => await client.ThreeDAsync(commandCancellationToken),
            _ when command.Equals(EntitySettingsConstants.PictureAdjustment, StringComparison.OrdinalIgnoreCase) => await client.PictureAdjustmentAsync(commandCancellationToken),
            _ when command.Equals(EntitySettingsConstants.Hdr, StringComparison.OrdinalIgnoreCase) => await client.HDRAsync(commandCancellationToken),
            _ when command.Equals(EntitySettingsConstants.InfoHold, StringComparison.OrdinalIgnoreCase) => await client.InfoHoldAsync(commandCancellationToken),
            _ when command.Equals(EntitySettingsConstants.ResolutionHold, StringComparison.OrdinalIgnoreCase) => await client.ResolutionHoldAsync(commandCancellationToken),
            _ when command.Equals(EntitySettingsConstants.AvSync, StringComparison.OrdinalIgnoreCase) => await client.AVSyncAsync(commandCancellationToken),
            _ when command.Equals(EntitySettingsConstants.GaplessPlay, StringComparison.OrdinalIgnoreCase) => await client.GaplessPlayAsync(commandCancellationToken),
            _ when command.Equals(EntitySettingsConstants.InfoToggle, StringComparison.OrdinalIgnoreCase) => await client.InfoToggleAsync(commandCancellationToken),
            _ when command.Equals(MediaPlayerCommandIdConstants.AudioTrack, StringComparison.OrdinalIgnoreCase) => await client.AudioAsync(commandCancellationToken),
            _ when command.Equals(MediaPlayerCommandIdConstants.OpenClose, StringComparison.OrdinalIgnoreCase) => (bool)await client.EjectToggleAsync(commandCancellationToken),

            _ => false
        };

        return result ? EntityCommandResult.Other : EntityCommandResult.Failure;
    }

    private static async ValueTask<OppoResult<PowerState>> HandlePowerOnAsync(OppoClientHolder oppoClientHolder,
        CancellationTokenWrapper cancellationTokenWrapper,
        CancellationToken cancellationToken)
    {
        cancellationTokenWrapper.EnsureNonCancelledBroadcastCancellationTokenSource();
        var powerStateResponse = await oppoClientHolder.Client.PowerOnAsync(cancellationToken);
        // Power commands can be flaky, so we try twice
        if (powerStateResponse is not { Result: PowerState.On })
            powerStateResponse = await oppoClientHolder.Client.PowerOnAsync(cancellationToken);
        return powerStateResponse;
    }

    private static async ValueTask<OppoResult<PowerState>> HandlePowerOffAsync(OppoClientHolder oppoClientHolder, CancellationToken cancellationToken)
    {
        // Power commands can be flaky, so we try twice
        var powerStateResponse = await oppoClientHolder.Client.PowerOffAsync(cancellationToken);
        if (powerStateResponse is not { Result: PowerState.Off })
            powerStateResponse = await oppoClientHolder.Client.PowerOffAsync(cancellationToken);

        return powerStateResponse;
    }
}