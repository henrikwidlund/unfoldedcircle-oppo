using System.Collections.Concurrent;
using System.Collections.Frozen;

using Microsoft.Extensions.Options;

using Oppo;

using UnfoldedCircle.Models.Events;
using UnfoldedCircle.Models.Shared;
using UnfoldedCircle.Models.Sync;
using UnfoldedCircle.OppoBluRay.AlbumCover;
using UnfoldedCircle.OppoBluRay.Configuration;
using UnfoldedCircle.OppoBluRay.Json;
using UnfoldedCircle.OppoBluRay.OppoEntity;
using UnfoldedCircle.OppoBluRay.Response;
using UnfoldedCircle.Server.Configuration;
using UnfoldedCircle.Server.DependencyInjection;
using UnfoldedCircle.Server.Extensions;
using UnfoldedCircle.Server.Response;
using UnfoldedCircle.Server.WebSocket;

namespace UnfoldedCircle.OppoBluRay.WebSocket;

public partial class OppoWebSocketHandler(
    IOppoClientFactory oppoClientFactory,
    IAlbumCoverService albumCoverService,
    IConfigurationService<OppoConfigurationItem> configurationService,
    IOptions<UnfoldedCircleOptions> options,
    ILogger<UnfoldedCircleWebSocketHandler<OppoCommandId, OppoConfigurationItem>> logger)
    : UnfoldedCircleWebSocketHandler<OppoCommandId, OppoConfigurationItem>(configurationService, options, logger)
{
    private readonly IOppoClientFactory _oppoClientFactory = oppoClientFactory;
    private readonly IAlbumCoverService _albumCoverService = albumCoverService;

    protected override FrozenSet<EntityType> SupportedEntityTypes { get; } = [EntityType.MediaPlayer, EntityType.Remote];

    protected override ValueTask<DeviceState> OnGetDeviceStateAsync(GetDeviceStateMsg payload, string wsId, CancellationToken cancellationToken)
        => ValueTask.FromResult(DeviceState.Connected);


    protected override async ValueTask<EntityState> GetEntityStateAsync(OppoConfigurationItem entity,
        string wsId,
        CancellationToken cancellationToken)
    {
        var oppoClientHolder = await TryGetOppoClientHolder(wsId, entity.EntityId, IdentifierType.EntityId, cancellationToken);
        if (oppoClientHolder is null)
            return EntityState.Disconnected;

        return await oppoClientHolder.Client.IsConnectedAsync() ? EntityState.Connected : EntityState.Disconnected;
    }

    protected override async ValueTask<IReadOnlyCollection<AvailableEntity>> OnGetAvailableEntitiesAsync(GetAvailableEntitiesMsg payload, string wsId, CancellationToken cancellationToken)
    {
        var entities = await GetEntities(wsId, payload.MsgData.Filter?.DeviceId, cancellationToken);
        return GetAvailableEntities(entities, payload).ToArray();
    }

    protected override async ValueTask OnSubscribeEventsAsync(System.Net.WebSockets.WebSocket socket, CommonReq payload, string wsId, CancellationTokenWrapper cancellationTokenWrapper)
    {
        var oppoClientHolders = await TryGetOppoClientHolders(wsId, cancellationTokenWrapper.RequestAborted);
        if (oppoClientHolders is { Count: > 0 })
        {
            foreach (var oppoClientHolder in oppoClientHolders)
            {
                if (!IsBroadcastingEvents(oppoClientHolder.ClientKey.EntityId) && await oppoClientHolder.Client.IsConnectedAsync())
                    _ = HandleEventUpdatesAsync(socket, oppoClientHolder.ClientKey.EntityId, wsId, cancellationTokenWrapper);

                await SendMessageAsync(socket,
                    ResponsePayloadHelpers.CreateStateChangedResponsePayload(
                        new MediaPlayerStateChangedEventMessageDataAttributes { SourceList = OppoEntitySettings.SourceList[oppoClientHolder.ClientKey.Model] },
                        oppoClientHolder.ClientKey.HostName,
                        EntityType.MediaPlayer
                    ),
                    wsId,
                    cancellationTokenWrapper.RequestAborted);
            }
        }
    }

    protected override ValueTask OnUnsubscribeEventsAsync(UnsubscribeEventsMsg payload, string wsId, CancellationToken cancellationToken)
        => ValueTask.CompletedTask;

    private static IEnumerable<AvailableEntity> GetAvailableEntities(
        List<OppoConfigurationItem>? entities,
        GetAvailableEntitiesMsg payload)
    {
        if (entities is not { Count: > 0 })
            yield break;

        var hasDeviceIdFilter = !string.IsNullOrEmpty(payload.MsgData.Filter?.DeviceId);
        var hasEntityTypeFilter = payload.MsgData.Filter?.EntityType is not null;
        foreach (var unfoldedCircleConfigurationItem in entities)
        {
            if (hasDeviceIdFilter && !string.Equals(unfoldedCircleConfigurationItem.DeviceId, payload.MsgData.Filter!.DeviceId.GetNullableBaseIdentifier(), StringComparison.OrdinalIgnoreCase))
                continue;

            if (hasEntityTypeFilter)
            {
                if (payload.MsgData.Filter!.EntityType == EntityType.MediaPlayer)
                    yield return GetMediaPlayerEntity(unfoldedCircleConfigurationItem);
                else if (payload.MsgData.Filter.EntityType == EntityType.Remote)
                    yield return GetRemoteEntity(unfoldedCircleConfigurationItem);
            }
            else
            {
                yield return GetMediaPlayerEntity(unfoldedCircleConfigurationItem);
                yield return GetRemoteEntity(unfoldedCircleConfigurationItem);
            }
        }
    }

    private static MediaPlayerAvailableEntity GetMediaPlayerEntity(OppoConfigurationItem configurationItem) =>
        new()
        {
            EntityId = configurationItem.EntityId.GetIdentifier(EntityType.MediaPlayer),
            EntityType = EntityType.MediaPlayer,
            Name = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["en"] = configurationItem.EntityName },
            DeviceId = configurationItem.DeviceId.GetNullableIdentifier(EntityType.MediaPlayer),
            Features = OppoEntitySettings.MediaPlayerEntityFeatures,
            Options = new Dictionary<string, ISet<string>>(StringComparer.OrdinalIgnoreCase) { ["simple_commands"] = OppoEntitySettings.MediaPlayerSimpleCommands }
        };

    private static RemoteAvailableEntity GetRemoteEntity(OppoConfigurationItem configurationItem) =>
        new()
        {
            EntityId = configurationItem.EntityId.GetIdentifier(EntityType.Remote),
            EntityType = EntityType.Remote,
            Name = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["en"] = $"{configurationItem.EntityName} Remote" },
            DeviceId = configurationItem.DeviceId.GetNullableIdentifier(EntityType.Remote),
            Features = OppoEntitySettings.RemoteFeatures,
            Options = OppoEntitySettings.RemoteOptions
        };

    protected override async ValueTask<EntityStateChanged[]> OnGetEntityStatesAsync(GetEntityStatesMsg payload, string wsId, CancellationToken cancellationToken)
    {
        var entities = await GetEntities(wsId, payload.MsgData?.DeviceId, cancellationToken);
        return entities is null
            ? []
            : OppoResponsePayloadHelpers.GetEntityStates(entities.Select(static x => new EntityIdDeviceId(x.EntityId, x.DeviceId, x.Model))).ToArray();
    }

    protected override async ValueTask<OnSetupResult?> OnSetupDriverAsync(
        SetupDriverMsg payload,
        string wsId,
        CancellationToken cancellationToken)
    {
        var configuration = await _configurationService.GetConfigurationAsync(cancellationToken);
        var driverMetadata = await _configurationService.GetDriverMetadataAsync(cancellationToken);
        var host = payload.MsgData.SetupData[OppoConstants.IpAddressKey];
        var oppoModel = GetOppoModel(payload.MsgData.SetupData);
        var entityName = payload.MsgData.SetupData.GetValueOrNull(OppoConstants.EntityName, $"{driverMetadata.Name["en"]} ({GetOppoModelName(oppoModel)}) - {host}");
        var deviceId = payload.MsgData.SetupData.GetValueOrNull(OppoConstants.DeviceIdKey, host);
        bool? useMediaEvents = payload.MsgData.SetupData.TryGetValue(OppoConstants.UseMediaEventsKey, out var useMediaEventsValue)
            ? useMediaEventsValue.Equals(bool.TrueString, StringComparison.OrdinalIgnoreCase)
            : null;

        bool? useChapterLengthForMovies = payload.MsgData.SetupData.TryGetValue(OppoConstants.ChapterOrMovieLengthKey, out var chapterOrMovieLengthValue)
                                        ? chapterOrMovieLengthValue.Equals(OppoConstants.ChapterLengthValue, StringComparison.OrdinalIgnoreCase)
                                        : null;

        var entity = configuration.Entities.Find(x => x.EntityId.Equals(host, StringComparison.Ordinal));
        if (entity is null)
        {
            _logger.LogInformation("Adding configuration for entity_id '{EntityId}'", host);
            entity = new OppoConfigurationItem
            {
                Host = host,
                Model = oppoModel,
                DeviceId = deviceId,
                EntityName = entityName,
                EntityId = host,
                UseMediaEvents = useMediaEvents ?? false,
                UseChapterLengthForMovies = useChapterLengthForMovies ?? false
            };
        }
        else
        {
            _logger.LogInformation("Updating configuration for entity_id '{EntityId}'", deviceId);
            configuration.Entities.Remove(entity);
            entity = entity with
            {
                Host = host,
                UseChapterLengthForMovies = useChapterLengthForMovies ?? entity.UseChapterLengthForMovies,
                UseMediaEvents = useMediaEvents ?? entity.UseMediaEvents,
                EntityName = entityName,
                Model = oppoModel
            };
        }

        configuration.Entities.Add(entity);

        await _configurationService.UpdateConfigurationAsync(configuration, cancellationToken);

        var oppoClientHolder = await TryGetOppoClientHolder(entity, cancellationToken);
        if (oppoClientHolder is not null && await oppoClientHolder.Client.IsConnectedAsync())
        {
            return new OnSetupResult(entity, SetupDriverResult.Finalized);
        }

        return new OnSetupResult(entity, SetupDriverResult.Error);

        static OppoModel GetOppoModel(Dictionary<string, string> msgDataSetupData)
        {
            if (msgDataSetupData.TryGetValue(OppoConstants.OppoModelKey, out var oppoModel))
            {
                return oppoModel switch
                {
                    _ when oppoModel.Equals(nameof(OppoModel.BDP8395), StringComparison.OrdinalIgnoreCase) => OppoModel.BDP8395,
                    _ when oppoModel.Equals(nameof(OppoModel.BDP10X), StringComparison.OrdinalIgnoreCase) => OppoModel.BDP10X,
                    _ when oppoModel.Equals(nameof(OppoModel.UDP203), StringComparison.OrdinalIgnoreCase) => OppoModel.UDP203,
                    _ => OppoModel.UDP205
                };
            }

            return OppoModel.UDP203;
        }

        static string GetOppoModelName(in OppoModel oppoModel) =>
            oppoModel switch
            {
                OppoModel.BDP8395 => "BDP-83/95",
                OppoModel.BDP10X => "BDP-10X",
                OppoModel.UDP203 => "UDP-203",
                _ => "UDP-205"
            };
    }

    protected override ValueTask OnSetupDriverUserDataAsync(System.Net.WebSockets.WebSocket socket, SetDriverUserDataMsg payload, string wsId, CancellationToken cancellationToken)
        => ValueTask.CompletedTask;

    protected override MediaPlayerEntityCommandMsgData<OppoCommandId>? DeserializeMediaPlayerCommandPayload(JsonDocument jsonDocument)
        => jsonDocument.Deserialize(OppoJsonSerializerContext.Instance.MediaPlayerEntityCommandMsgDataOppoCommandId);

    protected override ValueTask OnConnectAsync(ConnectEvent payload, string wsId, CancellationToken cancellationToken) => ValueTask.CompletedTask;

    protected override ValueTask<bool> OnDisconnectAsync(DisconnectEvent payload, string wsId, CancellationToken cancellationToken)
        => TryDisconnectOppoClients(wsId, payload.MsgData?.DeviceId, cancellationToken);

    protected override ValueTask OnAbortDriverSetupAsync(AbortDriverSetupEvent payload, string wsId, CancellationToken cancellationToken)
        => ValueTask.CompletedTask;

    protected override ValueTask OnEnterStandbyAsync(EnterStandbyEvent payload, string wsId, CancellationToken cancellationToken)
    {
        _oppoClientFactory.TryDisposeAllClients();
        return ValueTask.CompletedTask;
    }

    protected override ValueTask OnExitStandbyAsync(ExitStandbyEvent payload, string wsId, CancellationToken cancellationToken)
        => ValueTask.CompletedTask;

    protected override async ValueTask<bool> IsEntityReachableAsync(string wsId, string entityId, CancellationToken cancellationToken)
    {
        var oppoClientHolder = await TryGetOppoClientHolder(wsId, entityId, IdentifierType.EntityId, cancellationToken);
        return oppoClientHolder is not null && await oppoClientHolder.Client.IsConnectedAsync();
    }

    protected override async ValueTask<EntityCommandResult> OnMediaPlayerCommandAsync(System.Net.WebSockets.WebSocket socket, MediaPlayerEntityCommandMsgData<OppoCommandId> payload, string wsId, CancellationTokenWrapper cancellationTokenWrapper)
    {
        var oppoClientHolder = await TryGetOppoClientHolder(wsId, payload.MsgData.EntityId, IdentifierType.EntityId, cancellationTokenWrapper.RequestAborted);
        if (oppoClientHolder is null)
        {
            _logger.LogWarning("[{WSId}] WS: Could not find Oppo client for entity ID '{EntityId}'", wsId, payload.MsgData.EntityId);
            return EntityCommandResult.Failure;
        }

        OppoResult<PowerState>? powerState = payload.MsgData.CommandId switch
        {
            OppoCommandId.On => await HandlePowerOn(oppoClientHolder, cancellationTokenWrapper),
            OppoCommandId.Off => await HandlePowerOff(oppoClientHolder, cancellationTokenWrapper),
            OppoCommandId.Toggle => await oppoClientHolder.Client.PowerToggleAsync(cancellationTokenWrapper.RequestAborted),
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
                await oppoClientHolder.Client.PauseAsync(cancellationTokenWrapper.RequestAborted);
                break;
            case OppoCommandId.Stop:
                await oppoClientHolder.Client.StopAsync(cancellationTokenWrapper.RequestAborted);
                break;
            case OppoCommandId.Previous:
                await oppoClientHolder.Client.PreviousAsync(cancellationTokenWrapper.RequestAborted);
                break;
            case OppoCommandId.Next:
                await oppoClientHolder.Client.NextAsync(cancellationTokenWrapper.RequestAborted);
                break;
            case OppoCommandId.FastForward:
                await oppoClientHolder.Client.ForwardAsync(cancellationTokenWrapper.RequestAborted);
                break;
            case OppoCommandId.Rewind:
                await oppoClientHolder.Client.ReverseAsync(cancellationTokenWrapper.RequestAborted);
                break;
            case OppoCommandId.Seek:
                if (payload.MsgData.Params is { MediaPosition: not null })
                {
                    var digits = GetDigits(payload.MsgData.Params.MediaPosition.Value);
                    await oppoClientHolder.Client.GoToAsync(cancellationTokenWrapper.RequestAborted);
                    foreach (uint digit in digits)
                    {
                        if (digit > 9)
                        {
                            await oppoClientHolder.Client.ClearAsync(cancellationTokenWrapper.RequestAborted);
                            await oppoClientHolder.Client.EnterAsync(cancellationTokenWrapper.RequestAborted);
                            return EntityCommandResult.Other;
                        }

                        await oppoClientHolder.Client.NumericInputAsync((ushort)digit, cancellationTokenWrapper.RequestAborted);
                    }

                    await oppoClientHolder.Client.EnterAsync(cancellationTokenWrapper.RequestAborted);
                }
                break;
            case OppoCommandId.VolumeUp:
                await oppoClientHolder.Client.VolumeUpAsync(cancellationTokenWrapper.RequestAborted);
                break;
            case OppoCommandId.VolumeDown:
                await oppoClientHolder.Client.VolumeDownAsync(cancellationTokenWrapper.RequestAborted);
                break;
            case OppoCommandId.MuteToggle:
                await oppoClientHolder.Client.MuteToggleAsync(cancellationTokenWrapper.RequestAborted);
                break;
            case OppoCommandId.Repeat:
                if (payload.MsgData.Params is { Repeat: not null })
                    await oppoClientHolder.Client.SetRepeatAsync(payload.MsgData.Params.Repeat switch
                    {
                        Models.Shared.RepeatMode.Off => Oppo.RepeatMode.Off,
                        Models.Shared.RepeatMode.All => Oppo.RepeatMode.All,
                        Models.Shared.RepeatMode.One => Oppo.RepeatMode.Title,
                        _ => Oppo.RepeatMode.Off
                    }, cancellationTokenWrapper.RequestAborted);
                else
                    await oppoClientHolder.Client.RepeatAsync(cancellationTokenWrapper.RequestAborted);

                break;
            case OppoCommandId.ChannelUp:
                await oppoClientHolder.Client.PageUpAsync(cancellationTokenWrapper.RequestAborted);
                break;
            case OppoCommandId.ChannelDown:
                await oppoClientHolder.Client.PageDownAsync(cancellationTokenWrapper.RequestAborted);
                break;
            case OppoCommandId.CursorUp:
                await oppoClientHolder.Client.UpArrowAsync(cancellationTokenWrapper.RequestAborted);
                break;
            case OppoCommandId.CursorDown:
                await oppoClientHolder.Client.DownArrowAsync(cancellationTokenWrapper.RequestAborted);
                break;
            case OppoCommandId.CursorLeft:
                await oppoClientHolder.Client.LeftArrowAsync(cancellationTokenWrapper.RequestAborted);
                break;
            case OppoCommandId.CursorRight:
                await oppoClientHolder.Client.RightArrowAsync(cancellationTokenWrapper.RequestAborted);
                break;
            case OppoCommandId.CursorEnter:
                await oppoClientHolder.Client.EnterAsync(cancellationTokenWrapper.RequestAborted);
                break;
            case OppoCommandId.Digit0:
                await oppoClientHolder.Client.NumericInputAsync(0, cancellationTokenWrapper.RequestAborted);
                break;
            case OppoCommandId.Digit1:
                await oppoClientHolder.Client.NumericInputAsync(1, cancellationTokenWrapper.RequestAborted);
                break;
            case OppoCommandId.Digit2:
                await oppoClientHolder.Client.NumericInputAsync(2, cancellationTokenWrapper.RequestAborted);
                break;
            case OppoCommandId.Digit3:
                await oppoClientHolder.Client.NumericInputAsync(3, cancellationTokenWrapper.RequestAborted);
                break;
            case OppoCommandId.Digit4:
                await oppoClientHolder.Client.NumericInputAsync(4, cancellationTokenWrapper.RequestAborted);
                break;
            case OppoCommandId.Digit5:
                await oppoClientHolder.Client.NumericInputAsync(5, cancellationTokenWrapper.RequestAborted);
                break;
            case OppoCommandId.Digit6:
                await oppoClientHolder.Client.NumericInputAsync(6, cancellationTokenWrapper.RequestAborted);
                break;
            case OppoCommandId.Digit7:
                await oppoClientHolder.Client.NumericInputAsync(7, cancellationTokenWrapper.RequestAborted);
                break;
            case OppoCommandId.Digit8:
                await oppoClientHolder.Client.NumericInputAsync(8, cancellationTokenWrapper.RequestAborted);
                break;
            case OppoCommandId.Digit9:
                await oppoClientHolder.Client.NumericInputAsync(9, cancellationTokenWrapper.RequestAborted);
                break;
            case OppoCommandId.FunctionRed:
                await oppoClientHolder.Client.RedAsync(cancellationTokenWrapper.RequestAborted);
                break;
            case OppoCommandId.FunctionGreen:
                await oppoClientHolder.Client.GreenAsync(cancellationTokenWrapper.RequestAborted);
                break;
            case OppoCommandId.FunctionYellow:
                await oppoClientHolder.Client.YellowAsync(cancellationTokenWrapper.RequestAborted);
                break;
            case OppoCommandId.FunctionBlue:
                await oppoClientHolder.Client.BlueAsync(cancellationTokenWrapper.RequestAborted);
                break;
            case OppoCommandId.Home:
                await oppoClientHolder.Client.HomeAsync(cancellationTokenWrapper.RequestAborted);
                break;
            case OppoCommandId.ContextMenu:
                await oppoClientHolder.Client.TopMenuAsync(cancellationTokenWrapper.RequestAborted);
                break;
            case OppoCommandId.Info:
                await oppoClientHolder.Client.InfoToggleAsync(cancellationTokenWrapper.RequestAborted);
                break;
            case OppoCommandId.Back:
                await oppoClientHolder.Client.ReturnAsync(cancellationTokenWrapper.RequestAborted);
                break;
            case OppoCommandId.SelectSource:
                if (payload.MsgData.Params is { Source: not null } && OppoEntitySettings.SourceMap.TryGetValue(payload.MsgData.Params.Source, out var source))
                {
                    // Sending input source is only allowed if the unit is on - avoid locking up the driver by only sending it when the unit is ready
                    var currentPowerState = await oppoClientHolder.Client.QueryPowerStatusAsync(cancellationTokenWrapper.RequestAborted);
                    if (currentPowerState is { Result: PowerState.On })
                        await oppoClientHolder.Client.SetInputSourceAsync(source, cancellationTokenWrapper.RequestAborted);
                }
                else
                    await oppoClientHolder.Client.InputAsync(cancellationTokenWrapper.RequestAborted);
                break;
            case OppoCommandId.PureAudioToggle:
                await oppoClientHolder.Client.PureAudioToggleAsync(cancellationTokenWrapper.RequestAborted);
                break;
            case OppoCommandId.OpenClose:
                await oppoClientHolder.Client.EjectToggleAsync(cancellationTokenWrapper.RequestAborted);
                break;
            case OppoCommandId.AudioTrack:
                await oppoClientHolder.Client.AudioAsync(cancellationTokenWrapper.RequestAborted);
                break;
            case OppoCommandId.Subtitle:
                await oppoClientHolder.Client.SubtitleAsync(cancellationTokenWrapper.RequestAborted);
                break;
            case OppoCommandId.Settings:
                await oppoClientHolder.Client.SetupAsync(cancellationTokenWrapper.RequestAborted);
                break;
            case OppoCommandId.Dimmer:
                await oppoClientHolder.Client.DimmerAsync(cancellationTokenWrapper.RequestAborted);
                break;
            case OppoCommandId.Clear:
                await oppoClientHolder.Client.ClearAsync(cancellationTokenWrapper.RequestAborted);
                break;
            case OppoCommandId.PopUpMenu:
                await oppoClientHolder.Client.PopUpMenuAsync(cancellationTokenWrapper.RequestAborted);
                break;
            case OppoCommandId.Pause:
                await oppoClientHolder.Client.PauseAsync(cancellationTokenWrapper.RequestAborted);
                break;
            case OppoCommandId.Play:
                await oppoClientHolder.Client.PlayAsync(cancellationTokenWrapper.RequestAborted);
                break;
            case OppoCommandId.Angle:
                await oppoClientHolder.Client.AngleAsync(cancellationTokenWrapper.RequestAborted);
                break;
            case OppoCommandId.Zoom:
                await oppoClientHolder.Client.ZoomAsync(cancellationTokenWrapper.RequestAborted);
                break;
            case OppoCommandId.SecondaryAudioProgram:
                await oppoClientHolder.Client.SecondaryAudioProgramAsync(cancellationTokenWrapper.RequestAborted);
                break;
            case OppoCommandId.AbReplay:
                await oppoClientHolder.Client.ABReplayAsync(cancellationTokenWrapper.RequestAborted);
                break;
            case OppoCommandId.PictureInPicture:
                await oppoClientHolder.Client.PictureInPictureAsync(cancellationTokenWrapper.RequestAborted);
                break;
            case OppoCommandId.Resolution:
                await oppoClientHolder.Client.ResolutionAsync(cancellationTokenWrapper.RequestAborted);
                break;
            case OppoCommandId.SubtitleHold:
                await oppoClientHolder.Client.SubtitleHoldAsync(cancellationTokenWrapper.RequestAborted);
                break;
            case OppoCommandId.Option:
                await oppoClientHolder.Client.OptionAsync(cancellationTokenWrapper.RequestAborted);
                break;
            case OppoCommandId.ThreeD:
                await oppoClientHolder.Client.ThreeDAsync(cancellationTokenWrapper.RequestAborted);
                break;
            case OppoCommandId.PictureAdjustment:
                await oppoClientHolder.Client.PictureAdjustmentAsync(cancellationTokenWrapper.RequestAborted);
                break;
            case OppoCommandId.Hdr:
                await oppoClientHolder.Client.HDRAsync(cancellationTokenWrapper.RequestAborted);
                break;
            case OppoCommandId.InfoHold:
                await oppoClientHolder.Client.InfoHoldAsync(cancellationTokenWrapper.RequestAborted);
                break;
            case OppoCommandId.ResolutionHold:
                await oppoClientHolder.Client.ResolutionHoldAsync(cancellationTokenWrapper.RequestAborted);
                break;
            case OppoCommandId.AvSync:
                await oppoClientHolder.Client.AVSyncAsync(cancellationTokenWrapper.RequestAborted);
                break;
            case OppoCommandId.GaplessPlay:
                await oppoClientHolder.Client.GaplessPlayAsync(cancellationTokenWrapper.RequestAborted);
                break;

            case OppoCommandId.Shuffle:
                if (payload.MsgData.Params is { Shuffle: not null })
                    await oppoClientHolder.Client.SetRepeatAsync(payload.MsgData.Params.Shuffle.Value ? Oppo.RepeatMode.Shuffle : Oppo.RepeatMode.Off, cancellationTokenWrapper.RequestAborted);
                break;

            case OppoCommandId.Volume:
                if (payload.MsgData.Params is { Volume: not null })
                    await oppoClientHolder.Client.SetVolumeAsync(payload.MsgData.Params.Volume.Value, cancellationTokenWrapper.RequestAborted);
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

        return success ? EntityCommandResult.Other : EntityCommandResult.Failure;
    }

    protected override async ValueTask<EntityCommandResult> OnRemoteCommandAsync(System.Net.WebSockets.WebSocket socket, RemoteEntityCommandMsgData payload, string command, string wsId, CancellationTokenWrapper cancellationTokenWrapper)
    {
        var oppoClientHolder = await TryGetOppoClientHolder(wsId, payload.MsgData.EntityId, IdentifierType.EntityId, cancellationTokenWrapper.RequestAborted);
        if (oppoClientHolder is null)
        {
            _logger.LogWarning("[{WSId}] WS: Could not find Oppo client for entity ID '{EntityId}'", wsId, payload.MsgData.EntityId);
            return EntityCommandResult.Failure;
        }

        var client = oppoClientHolder.Client;
        var cancellationToken = cancellationTokenWrapper.RequestAborted;
        OppoResult<PowerState>? powerState = command switch
        {
            _ when command.Equals(RemoteButtonConstants.On, StringComparison.OrdinalIgnoreCase) => await HandlePowerOn(oppoClientHolder, cancellationTokenWrapper),
            _ when command.Equals(RemoteButtonConstants.Off, StringComparison.OrdinalIgnoreCase) => await HandlePowerOff(oppoClientHolder, cancellationTokenWrapper),
            _ when command.Equals(RemoteButtonConstants.Toggle, StringComparison.OrdinalIgnoreCase) => await client.PowerToggleAsync(cancellationToken),
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
            _ when command.Equals(MediaPlayerCommandIdConstants.PlayPause, StringComparison.OrdinalIgnoreCase) => await client.PauseAsync(cancellationToken),
            _ when command.Equals(RemoteButtonConstants.Previous, StringComparison.OrdinalIgnoreCase) => await client.PreviousAsync(cancellationToken),
            _ when command.Equals(RemoteButtonConstants.Next, StringComparison.OrdinalIgnoreCase) => await client.NextAsync(cancellationToken),
            _ when command.Equals(MediaPlayerCommandIdConstants.FastForward, StringComparison.OrdinalIgnoreCase) => (bool)await client.ForwardAsync(cancellationToken),
            _ when command.Equals(MediaPlayerCommandIdConstants.Rewind, StringComparison.OrdinalIgnoreCase) => (bool)await client.ReverseAsync(cancellationToken),
            _ when command.Equals(RemoteButtonConstants.VolumeUp, StringComparison.OrdinalIgnoreCase) => (bool)await client.VolumeUpAsync(cancellationToken),
            _ when command.Equals(RemoteButtonConstants.VolumeDown, StringComparison.OrdinalIgnoreCase) => (bool)await client.VolumeDownAsync(cancellationToken),
            _ when command.Equals(RemoteButtonConstants.Mute, StringComparison.OrdinalIgnoreCase) => (bool)await client.MuteToggleAsync(cancellationToken),
            _ when command.Equals(MediaPlayerCommandIdConstants.Repeat, StringComparison.OrdinalIgnoreCase) => (bool)await client.RepeatAsync(cancellationToken),
            _ when command.Equals(RemoteButtonConstants.ChannelUp, StringComparison.OrdinalIgnoreCase) => await client.PageUpAsync(cancellationToken),
            _ when command.Equals(RemoteButtonConstants.ChannelDown, StringComparison.OrdinalIgnoreCase) => await client.PageDownAsync(cancellationToken),
            _ when command.Equals(RemoteButtonConstants.DpadUp, StringComparison.OrdinalIgnoreCase) => await client.UpArrowAsync(cancellationToken),
            _ when command.Equals(RemoteButtonConstants.DpadDown, StringComparison.OrdinalIgnoreCase) => await client.DownArrowAsync(cancellationToken),
            _ when command.Equals(RemoteButtonConstants.DpadLeft, StringComparison.OrdinalIgnoreCase) => await client.LeftArrowAsync(cancellationToken),
            _ when command.Equals(RemoteButtonConstants.DpadRight, StringComparison.OrdinalIgnoreCase) => await client.RightArrowAsync(cancellationToken),
            _ when command.Equals(RemoteButtonConstants.DpadMiddle, StringComparison.OrdinalIgnoreCase) => await client.EnterAsync(cancellationToken),
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
            _ when command.Equals(RemoteButtonConstants.Red, StringComparison.OrdinalIgnoreCase) => await client.RedAsync(cancellationToken),
            _ when command.Equals(RemoteButtonConstants.Green, StringComparison.OrdinalIgnoreCase) => await client.GreenAsync(cancellationToken),
            _ when command.Equals(RemoteButtonConstants.Yellow, StringComparison.OrdinalIgnoreCase) => await client.YellowAsync(cancellationToken),
            _ when command.Equals(RemoteButtonConstants.Blue, StringComparison.OrdinalIgnoreCase) => await client.BlueAsync(cancellationToken),
            _ when command.Equals(RemoteButtonConstants.Home, StringComparison.OrdinalIgnoreCase) => await client.HomeAsync(cancellationToken),
            _ when command.Equals(MediaPlayerCommandIdConstants.ContextMenu, StringComparison.OrdinalIgnoreCase) => await client.TopMenuAsync(cancellationToken),
            _ when command.Equals(MediaPlayerCommandIdConstants.Info, StringComparison.OrdinalIgnoreCase) => await client.InfoToggleAsync(cancellationToken),
            _ when command.Equals(RemoteButtonConstants.Back, StringComparison.OrdinalIgnoreCase) => await client.ReturnAsync(cancellationToken),
            _ when command.Equals(MediaPlayerCommandIdConstants.Eject, StringComparison.OrdinalIgnoreCase) => (bool)await client.EjectToggleAsync(cancellationToken),
            _ when command.Equals(MediaPlayerCommandIdConstants.Subtitle, StringComparison.OrdinalIgnoreCase) => await client.SubtitleAsync(cancellationToken),
            _ when command.Equals(MediaPlayerCommandIdConstants.Settings, StringComparison.OrdinalIgnoreCase) => await client.SetupAsync(cancellationToken),
            _ when command.Equals(RemoteButtonConstants.Power, StringComparison.OrdinalIgnoreCase) => (bool)await client.PowerToggleAsync(cancellationToken),
            _ when command.Equals(EntitySettingsConstants.Dimmer, StringComparison.OrdinalIgnoreCase) => (bool)await client.DimmerAsync(cancellationToken),
            _ when command.Equals(EntitySettingsConstants.PureAudioToggle, StringComparison.OrdinalIgnoreCase) => (bool)await client.PureAudioToggleAsync(cancellationToken),
            _ when command.Equals(EntitySettingsConstants.Clear, StringComparison.OrdinalIgnoreCase) => await client.ClearAsync(cancellationToken),
            _ when command.Equals(EntitySettingsConstants.TopMenu, StringComparison.OrdinalIgnoreCase) => await client.TopMenuAsync(cancellationToken),
            _ when command.Equals(EntitySettingsConstants.PopUpMenu, StringComparison.OrdinalIgnoreCase) => await client.PopUpMenuAsync(cancellationToken),
            _ when command.Equals(EntitySettingsConstants.Pause, StringComparison.OrdinalIgnoreCase) => await client.PauseAsync(cancellationToken),
            _ when command.Equals(EntitySettingsConstants.Play, StringComparison.OrdinalIgnoreCase) => await client.PlayAsync(cancellationToken),
            _ when command.Equals(EntitySettingsConstants.Angle, StringComparison.OrdinalIgnoreCase) => (bool)await client.AngleAsync(cancellationToken),
            _ when command.Equals(EntitySettingsConstants.Zoom, StringComparison.OrdinalIgnoreCase) => (bool)await client.ZoomAsync(cancellationToken),
            _ when command.Equals(EntitySettingsConstants.SecondaryAudioProgram, StringComparison.OrdinalIgnoreCase) => (bool)await client.SecondaryAudioProgramAsync(cancellationToken),
            _ when command.Equals(EntitySettingsConstants.AbReplay, StringComparison.OrdinalIgnoreCase) => (bool)await client.ABReplayAsync(cancellationToken),
            _ when command.Equals(EntitySettingsConstants.PictureInPicture, StringComparison.OrdinalIgnoreCase) => (bool)await client.PictureInPictureAsync(cancellationToken),
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
            _ when command.Equals(EntitySettingsConstants.InfoToggle, StringComparison.OrdinalIgnoreCase) => await client.InfoToggleAsync(cancellationToken),
            _ when command.Equals(MediaPlayerCommandIdConstants.AudioTrack, StringComparison.OrdinalIgnoreCase) => await client.AudioAsync(cancellationToken),
            _ when command.Equals(MediaPlayerCommandIdConstants.OpenClose, StringComparison.OrdinalIgnoreCase) => (bool)await client.EjectToggleAsync(cancellationToken),

            _ => false
        };

        return result ? EntityCommandResult.Other : EntityCommandResult.Failure;
    }

    private static async ValueTask<OppoResult<PowerState>> HandlePowerOn(OppoClientHolder oppoClientHolder, CancellationTokenWrapper cancellationTokenWrapper)
    {
        cancellationTokenWrapper.EnsureNonCancelledBroadcastCancellationTokenSource();
        var powerStateResponse = await oppoClientHolder.Client.PowerOnAsync(cancellationTokenWrapper.RequestAborted);
        // Power commands can be flaky, so we try twice
        if (powerStateResponse is not { Result: PowerState.On })
            powerStateResponse = await oppoClientHolder.Client.PowerOnAsync(cancellationTokenWrapper.RequestAborted);
        return powerStateResponse;
    }

    private static async ValueTask<OppoResult<PowerState>> HandlePowerOff(OppoClientHolder oppoClientHolder, CancellationTokenWrapper cancellationTokenWrapper)
    {
        // Power commands can be flaky, so we try twice
        var powerStateResponse = await oppoClientHolder.Client.PowerOffAsync(cancellationTokenWrapper.RequestAborted);
        if (powerStateResponse is not { Result: PowerState.Off })
            powerStateResponse = await oppoClientHolder.Client.PowerOffAsync(cancellationTokenWrapper.RequestAborted);

        return powerStateResponse;
    }

    private static readonly ConcurrentDictionary<int, int> PreviousMediaStatesMap = new();
    private static readonly ConcurrentDictionary<int, State> PreviousRemoteStatesMap = new();

    private readonly SemaphoreSlim _broadcastSemaphoreSlim = new(1, 1);

    protected override async Task HandleEventUpdatesAsync(System.Net.WebSockets.WebSocket socket, string entityId, string wsId, CancellationTokenWrapper cancellationTokenWrapper)
    {
        if (!IsSocketSubscribedToEvents(wsId))
        {
            _logger.LogDebug("{WSId} Subscribe events not called", wsId);
            return;
        }

        var cancellationTokenSource = cancellationTokenWrapper.GetCurrentBroadcastCancellationTokenSource();
        if (cancellationTokenSource is null || cancellationTokenSource.IsCancellationRequested)
        {
            _logger.LogDebug("{WSId} Broadcast token is cancelled {IsCancellationRequested}", wsId, cancellationTokenSource?.IsCancellationRequested);
            return;
        }

        if (await _broadcastSemaphoreSlim.WaitAsync(TimeSpan.FromSeconds(1), cancellationTokenSource.Token))
        {
            try
            {
                if (IsBroadcastingEvents(entityId))
                {
                    _logger.LogDebug("{WSId} Events already running for {EntityId}", wsId, entityId);
                    return;
                }

                AddEntityIdToBroadcastingEvents(entityId);
            }
            finally
            {
                _broadcastSemaphoreSlim.Release();
            }
        }
        else
        {
            _logger.LogError("{WSId} Could not acquire semaphore for broadcasting events for {EntityId}. Will not start broadcasting.",
                wsId, entityId);
            return;
        }

        using var periodicTimer = new PeriodicTimer(TimeSpan.FromSeconds(1));

        var oppoClientHolder = await TryGetOppoClientHolder(wsId, entityId, IdentifierType.EntityId, cancellationTokenWrapper.RequestAborted);
        if (oppoClientHolder is null)
        {
            _logger.LogWarning("[{WSId}] WS: Could not find Oppo client for entity ID '{EntityId}'", wsId, entityId);
            return;
        }
        _logger.LogDebug("{WSId} Trying to get OppoClientHolder.", wsId);
        while (await periodicTimer.WaitForNextTickAsync(cancellationTokenSource.Token))
        {
            if (await oppoClientHolder.Client.IsConnectedAsync())
                break;
        }

        _logger.LogDebug("{WSId} Starting events for {DeviceId}", wsId, oppoClientHolder.Client.GetHost());
        try
        {
            while (await periodicTimer.WaitForNextTickAsync(cancellationTokenSource.Token))
            {
                var connected = await oppoClientHolder.Client.IsConnectedAsync();
                if (!connected)
                    _logger.LogDebug("{WSId} Client not connected. {@ClientKey}", wsId, oppoClientHolder.ClientKey);

                OppoResult<PowerState>? powerStatusResponse;
                if (connected)
                    powerStatusResponse = await oppoClientHolder.Client.QueryPowerStatusAsync(cancellationTokenSource.Token);
                else
                    powerStatusResponse = null;

                var state = powerStatusResponse switch
                {
                    { Result: PowerState.On } => State.On,
                    { Result: PowerState.Off } => State.Off,
                    _ => State.Unknown
                };

                MediaPlayerStateChangedEventMessageDataAttributes newMediaPlayerState;
                // Only send power state if not using media events
                if (oppoClientHolder is { ClientKey.UseMediaEvents: false })
                {
                    newMediaPlayerState = new MediaPlayerStateChangedEventMessageDataAttributes { State = state };
                    if (!await SendMediaPlayerEvent(socket, wsId, oppoClientHolder, newMediaPlayerState, cancellationTokenSource.Token))
                        continue;

                    await SendRemotePowerEvent(socket, wsId, oppoClientHolder, state, cancellationTokenSource.Token);

                    continue;
                }

                OppoResult<VolumeInfo>? volumeResponse = null;
                OppoResult<InputSource>? inputSourceResponse = null;
                OppoResult<DiscType>? discTypeResponse = null;
                OppoResult<uint>? elapsedResponse = null;
                OppoResult<uint>? remainingResponse = null;
                OppoResult<string>? trackResponse = null;
                string? album = null;
                string? performer = null;
                Uri? coverUri = null;
                bool? shuffle = null;
                Models.Shared.RepeatMode? repeatMode = null;

                if (powerStatusResponse is { Result: PowerState.On })
                {
                    volumeResponse = await oppoClientHolder.Client.QueryVolumeAsync(cancellationTokenSource.Token);
                    inputSourceResponse = await oppoClientHolder.Client.QueryInputSourceAsync(cancellationTokenSource.Token);

                    var playbackStatusResponse = await oppoClientHolder.Client.QueryPlaybackStatusAsync(cancellationTokenSource.Token);
                    state = playbackStatusResponse switch
                    {
                        { Result: PlaybackStatus.Unknown } => State.Unknown,
                        { Result: PlaybackStatus.Play } => State.Playing,
                        { Result: PlaybackStatus.Pause } => State.Paused,
                        { Result: PlaybackStatus.FastForward or PlaybackStatus.FastRewind or PlaybackStatus.SlowForward or PlaybackStatus.SlowRewind} => State.Buffering,
                        _ => State.On
                    };

                    if (playbackStatusResponse is { Result: PlaybackStatus.Play or PlaybackStatus.Pause })
                    {
                        discTypeResponse = await oppoClientHolder.Client.QueryDiscTypeAsync(cancellationTokenSource.Token);

                        if (discTypeResponse.Value && discTypeResponse.Value.Result is not DiscType.UnknownDisc and not DiscType.DataDisc)
                        {
                            (repeatMode, shuffle) = GetRepeatMode(await oppoClientHolder.Client.QueryRepeatModeAsync(cancellationTokenSource.Token));

                            if (discTypeResponse.Value.Result is DiscType.BlueRayMovie or DiscType.DVDVideo or DiscType.UltraHDBluRay)
                            {
                                elapsedResponse = oppoClientHolder.ClientKey.UseChapterLengthForMovies
                                    ? await oppoClientHolder.Client.QueryChapterElapsedTimeAsync(cancellationTokenSource.Token)
                                    : await oppoClientHolder.Client.QueryTotalElapsedTimeAsync(cancellationTokenSource.Token);
                                if (elapsedResponse.Value)
                                    remainingResponse = oppoClientHolder.ClientKey.UseChapterLengthForMovies
                                        ? await oppoClientHolder.Client.QueryChapterRemainingTimeAsync(cancellationTokenSource.Token)
                                        : await oppoClientHolder.Client.QueryTotalRemainingTimeAsync(cancellationTokenSource.Token);
                            }
                            else
                            {
                                elapsedResponse = await oppoClientHolder.Client.QueryTrackOrTitleElapsedTimeAsync(cancellationTokenSource.Token);
                                if (elapsedResponse.Value)
                                {
                                    remainingResponse = await oppoClientHolder.Client.QueryTrackOrTitleRemainingTimeAsync(cancellationTokenSource.Token);

                                    if (oppoClientHolder.ClientKey.Model is OppoModel.UDP203 or OppoModel.UDP205)
                                    {
                                        trackResponse = await oppoClientHolder.Client.QueryTrackNameAsync(cancellationTokenSource.Token);
                                        album = (await oppoClientHolder.Client.QueryTrackAlbumAsync(cancellationTokenSource.Token)).Result;
                                        performer = (await oppoClientHolder.Client.QueryTrackPerformerAsync(cancellationTokenSource.Token)).Result;
                                    }
                                }
                            }

                            if (!string.IsNullOrWhiteSpace(performer) && (!string.IsNullOrWhiteSpace(album) || !string.IsNullOrWhiteSpace(trackResponse?.Result)))
                            {
                                if (album?.StartsWith(performer, StringComparison.OrdinalIgnoreCase) is true && album.AsSpan()[performer.Length..].StartsWith("   ", StringComparison.Ordinal))
                                    album = album.AsSpan()[(performer.Length + 3)..].ToString();

                                coverUri = await _albumCoverService.GetAlbumCoverAsync(performer, album, null,
                                    cancellationTokenSource.Token);
                            }
                            else
                                coverUri = null;
                        }
                    }
                }

                newMediaPlayerState = new MediaPlayerStateChangedEventMessageDataAttributes
                {
                    State = state,
                    MediaType = discTypeResponse?.Result switch
                    {
                        DiscType.BlueRayMovie or DiscType.DVDVideo or DiscType.UltraHDBluRay => MediaType.Movie,
                        DiscType.DVDAudio or DiscType.SACD or DiscType.CDDiscAudio => MediaType.Music,
                        _ => null
                    },
                    MediaPosition = elapsedResponse?.Result,
                    MediaDuration = elapsedResponse?.Result + remainingResponse?.Result,
                    MediaTitle = ReplaceStarWithEllipsis(trackResponse?.Result),
                    MediaAlbum = ReplaceStarWithEllipsis(album),
                    MediaArtist = ReplaceStarWithEllipsis(performer),
                    MediaImageUrl = coverUri,
                    Repeat = repeatMode,
                    Shuffle = shuffle,
                    Source = GetInputSource(inputSourceResponse),
                    Volume = volumeResponse?.Result.Volume,
                    Muted = volumeResponse?.Result.Muted
                };

                if (!await SendMediaPlayerEvent(socket, wsId, oppoClientHolder, newMediaPlayerState, cancellationTokenSource.Token))
                    continue;

                await SendRemotePowerEvent(socket, wsId, oppoClientHolder, state, cancellationTokenSource.Token);
            }
        }
        finally
        {
            bool acquiredLock = await _broadcastSemaphoreSlim.WaitAsync(TimeSpan.FromSeconds(1), cancellationTokenSource.Token);
            try
            {
                RemoveEntityIdToBroadcastingEvents(entityId);
            }
            finally
            {
                if (acquiredLock)
                    _broadcastSemaphoreSlim.Release();
            }
        }

        _logger.LogDebug("{WSId} Stopping media updates for {DeviceId}", wsId, oppoClientHolder.Client.GetHost());
        return;

        static string? ReplaceStarWithEllipsis(string? input) =>
            string.IsNullOrWhiteSpace(input) ? input : input.Replace('*', '…');

        static string? GetInputSource(in OppoResult<InputSource>? inputSourceResponse)
        {
            if (inputSourceResponse is not { Success: true })
                return null;

            return inputSourceResponse.Value.Result switch
            {
                InputSource.Unknown => null,
                InputSource.BluRayPlayer => OppoConstants.InputSource.BluRayPlayer,
                InputSource.HDMIIn => OppoConstants.InputSource.HDMIIn,
                InputSource.ARCHDMIOut => OppoConstants.InputSource.ARCHDMIOut,
                InputSource.Optical => OppoConstants.InputSource.Optical,
                InputSource.Coaxial => OppoConstants.InputSource.Coaxial,
                InputSource.USBAudio => OppoConstants.InputSource.USBAudio,
                InputSource.HDMIFront => OppoConstants.InputSource.HDMIFront,
                InputSource.HDMIBack => OppoConstants.InputSource.HDMIBack,
                InputSource.ARCHDMIOut1 => OppoConstants.InputSource.ARCHDMIOut1,
                InputSource.ARCHDMIOut2 => OppoConstants.InputSource.ARCHDMIOut2,
                _ => null
            };
        }
    }

    private async Task<bool> SendMediaPlayerEvent(System.Net.WebSockets.WebSocket socket,
        string wsId,
        OppoClientHolder oppoClientHolder,
        MediaPlayerStateChangedEventMessageDataAttributes mediaPlayerState,
        CancellationToken cancellationToken)
    {
        var stateHash = mediaPlayerState.GetHashCode();
        int clientHashCode = oppoClientHolder.ClientKey.GetHashCode();
        if (PreviousMediaStatesMap.TryGetValue(clientHashCode, out var previousStateHash) &&
            previousStateHash == stateHash)
            return false;

        PreviousMediaStatesMap[clientHashCode] = stateHash;
        await SendMessageAsync(socket,
            ResponsePayloadHelpers.CreateStateChangedResponsePayload(
                mediaPlayerState,
                oppoClientHolder.ClientKey.EntityId,
                EntityType.MediaPlayer),
            wsId,
            cancellationToken);
        return true;
    }

    private async Task SendRemotePowerEvent(System.Net.WebSockets.WebSocket socket,
        string wsId,
        OppoClientHolder oppoClientHolder,
        State state,
        CancellationToken cancellationToken)
    {
        int clientHashCode = oppoClientHolder.ClientKey.GetHashCode();
        if (PreviousRemoteStatesMap.TryGetValue(clientHashCode, out var previousState) &&
            previousState == state)
            return;

        PreviousRemoteStatesMap[clientHashCode] = state;
        await SendMessageAsync(socket,
            ResponsePayloadHelpers.CreateStateChangedResponsePayload(
                new RemoteStateChangedEventMessageDataAttributes { State = state switch
                {
                    State.Buffering or State.Playing or State.Paused or State.On => RemoteState.On,
                    State.Off => RemoteState.Off,
                    _ => RemoteState.Unknown
                } },
                oppoClientHolder.ClientKey.EntityId,
                EntityType.Remote),
            wsId,
            cancellationToken);
    }

    private static (Models.Shared.RepeatMode? RepeatMode, bool? shuffle) GetRepeatMode(OppoResult<CurrentRepeatMode> repeatModeResponse) =>
        !repeatModeResponse
            ? (null, null)
            : repeatModeResponse.Result switch
            {
                CurrentRepeatMode.Off => (Models.Shared.RepeatMode.Off, false),
                CurrentRepeatMode.RepeatOne => (Models.Shared.RepeatMode.One, false),
                CurrentRepeatMode.RepeatChapter => (Models.Shared.RepeatMode.One, false),
                CurrentRepeatMode.RepeatAll => (Models.Shared.RepeatMode.All, false),
                CurrentRepeatMode.RepeatTitle => (Models.Shared.RepeatMode.One, false),
                CurrentRepeatMode.Shuffle or CurrentRepeatMode.Random => (Models.Shared.RepeatMode.Off, true),
                _ => (Models.Shared.RepeatMode.Off, false)
            };

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