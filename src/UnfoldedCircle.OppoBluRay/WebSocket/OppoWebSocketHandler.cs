using System.Collections.Frozen;

using Microsoft.Extensions.Options;

using Oppo;

using UnfoldedCircle.Models.Events;
using UnfoldedCircle.Models.Shared;
using UnfoldedCircle.Models.Sync;
using UnfoldedCircle.OppoBluRay.AlbumCover;
using UnfoldedCircle.OppoBluRay.Configuration;
using UnfoldedCircle.OppoBluRay.Json;
using UnfoldedCircle.OppoBluRay.Logging;
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
        var oppoClientHolder = await TryGetOppoClientHolderAsync(wsId, entity.EntityId, IdentifierType.EntityId, cancellationToken);
        if (oppoClientHolder is null)
            return EntityState.Disconnected;

        return await oppoClientHolder.Client.IsConnectedAsync() ? EntityState.Connected : EntityState.Disconnected;
    }

    protected override async ValueTask<IReadOnlyCollection<AvailableEntity>> OnGetAvailableEntitiesAsync(GetAvailableEntitiesMsg payload, string wsId, CancellationToken cancellationToken)
        => GetAvailableEntities(await GetEntitiesAsync(wsId, payload.MsgData.Filter?.DeviceId, cancellationToken), payload).ToArray();

    protected override async ValueTask OnSubscribeEventsAsync(System.Net.WebSockets.WebSocket socket,
        CommonReq payload,
        string wsId,
        CancellationTokenWrapper cancellationTokenWrapper,
        CancellationToken commandCancellationToken)
    {
        if (await TryGetOppoClientHolders(wsId, commandCancellationToken) is { Count: > 0 } oppoClientHolders)
        {
            foreach (var oppoClientHolder in oppoClientHolders)
            {
                _ = Task.Factory.StartNew(() => HandleEventUpdatesAsync(socket, oppoClientHolder.ClientKey.EntityId, wsId, cancellationTokenWrapper),
                    TaskCreationOptions.LongRunning);

                await SendMessageAsync(socket,
                    ResponsePayloadHelpers.CreateStateChangedResponsePayload(
                        new MediaPlayerStateChangedEventMessageDataAttributes { SourceList = OppoEntitySettings.SourceList[oppoClientHolder.ClientKey.Model] },
                        oppoClientHolder.ClientKey.HostName,
                        EntityType.MediaPlayer
                    ),
                    wsId,
                    commandCancellationToken);
            }
        }
    }

    protected override async ValueTask OnUnsubscribeEventsAsync(UnsubscribeEventsMsg payload, string wsId, CancellationTokenWrapper cancellationTokenWrapper)
    {
        if (!string.IsNullOrEmpty(payload.MsgData?.DeviceId))
        {
            var oppoClientKey = await TryGetOppoClientKeyAsync(wsId, IdentifierType.DeviceId, payload.MsgData.DeviceId, cancellationTokenWrapper.ApplicationStopping);
            if (oppoClientKey is not null)
            {
                RemoveEntityIdToBroadcastingEvents(oppoClientKey.Value.EntityId, cancellationTokenWrapper);
                _oppoClientFactory.TryDisposeClient(oppoClientKey.Value);
            }
        }

        if (payload.MsgData?.EntityIds is { Length: > 0 })
        {
            foreach (string msgDataEntityId in payload.MsgData.EntityIds)
            {
                var oppoClientKey = await TryGetOppoClientKeyAsync(wsId, IdentifierType.EntityId, msgDataEntityId, cancellationTokenWrapper.ApplicationStopping);
                if (oppoClientKey is not null)
                {
                    RemoveEntityIdToBroadcastingEvents(oppoClientKey.Value.EntityId, cancellationTokenWrapper);
                    _oppoClientFactory.TryDisposeClient(oppoClientKey.Value);
                }
            }
        }

        // If no specific device or entity was specified, dispose all clients for this websocket ID.
        if (payload.MsgData is { DeviceId: null, EntityIds: null } &&
            await TryGetOppoClientKeysAsync(wsId, cancellationTokenWrapper.ApplicationStopping) is { } oppoClientKeys)
        {
            foreach (var oppoClientKey in oppoClientKeys)
            {
                RemoveEntityIdToBroadcastingEvents(oppoClientKey.EntityId, cancellationTokenWrapper);
                _oppoClientFactory.TryDisposeClient(oppoClientKey);
            }
        }
    }

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
            if (hasDeviceIdFilter)
            {
                var configDeviceId = unfoldedCircleConfigurationItem.DeviceId?.AsMemory();
                // we have a device id filter, so if the config device id is null, there is no match
                if (configDeviceId is null)
                    continue;
                if (!configDeviceId.Value.Span.Equals(payload.MsgData.Filter!.DeviceId.AsSpan().GetBaseIdentifier(), StringComparison.OrdinalIgnoreCase))
                    continue;
            }

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

        yield break;

        static MediaPlayerAvailableEntity GetMediaPlayerEntity(OppoConfigurationItem configurationItem) =>
            new()
            {
                EntityId = configurationItem.EntityId.GetIdentifier(EntityType.MediaPlayer),
                EntityType = EntityType.MediaPlayer,
                Name = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["en"] = configurationItem.EntityName },
                DeviceId = configurationItem.DeviceId.GetNullableIdentifier(EntityType.MediaPlayer),
                Features = OppoEntitySettings.MediaPlayerEntityFeatures,
                Options = new Dictionary<string, ISet<string>>(StringComparer.OrdinalIgnoreCase) { ["simple_commands"] = OppoEntitySettings.MediaPlayerSimpleCommands }
            };

        static RemoteAvailableEntity GetRemoteEntity(OppoConfigurationItem configurationItem) =>
            new()
            {
                EntityId = configurationItem.EntityId.GetIdentifier(EntityType.Remote),
                EntityType = EntityType.Remote,
                Name = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["en"] = $"{configurationItem.EntityName} Remote" },
                DeviceId = configurationItem.DeviceId.GetNullableIdentifier(EntityType.Remote),
                Features = OppoEntitySettings.RemoteFeatures,
                Options = OppoEntitySettings.RemoteOptions
            };
    }

    protected override async ValueTask<EntityStateChanged[]> OnGetEntityStatesAsync(GetEntityStatesMsg payload, string wsId, CancellationToken cancellationToken)
    {
        var entities = await GetEntitiesAsync(wsId, payload.MsgData?.DeviceId, cancellationToken);
        return entities is null
            ? []
            : OppoResponsePayloadHelpers.GetEntityStates(entities.Select(static x => new EntityIdDeviceId(x.EntityId, x.DeviceId, x.Model))).ToArray();
    }

    protected override ValueTask<SetupDriverUserDataResult> OnSetupDriverUserDataConfirmAsync(System.Net.WebSockets.WebSocket socket, SetDriverUserDataMsg payload, string wsId, CancellationToken cancellationToken)
        => ValueTask.FromResult(SetupDriverUserDataResult.Finalized);

    protected override async ValueTask<SettingsPage> CreateNewEntitySettingsPageAsync(CancellationToken cancellationToken)
    {
        var configuration = await _configurationService.GetConfigurationAsync(cancellationToken);
        return CreateSettingsPage(null, configuration.MaxMessageHandlingWaitTimeInSeconds ?? 9.5);
    }

    protected override async ValueTask<SettingsPage> CreateReconfigureEntitySettingsPageAsync(OppoConfigurationItem configurationItem, CancellationToken cancellationToken)
    {
        var configuration = await _configurationService.GetConfigurationAsync(cancellationToken);
        var settingsPage = CreateSettingsPage(configurationItem, configuration.MaxMessageHandlingWaitTimeInSeconds ?? 9.5);
        return settingsPage with
        {
            Settings = settingsPage.Settings.Where(static x =>
                !x.Id.Equals(OppoConstants.IpAddressKey, StringComparison.OrdinalIgnoreCase) &&
                !x.Id.Equals(OppoConstants.EntityName, StringComparison.OrdinalIgnoreCase)).ToArray()
        };
    }

    private static SettingsPage CreateSettingsPage(OppoConfigurationItem? configurationItem, double maxMessageHandlingWaitTimeInSeconds) =>
        new()
        {
            Title = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["en"] = configurationItem == null ? "Add a new device" : "Reconfigure device" },
            Settings = [
                new Setting
                {
                    Id = OppoConstants.EntityName,
                    Field = new SettingTypeText
                    {
                        Text = new ValueRegex()
                    },
                    Label = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["en"] = "Enter the name of the Oppo player (optional)" }
                },
                new Setting
                {
                    Id = OppoConstants.IpAddressKey,
                    Field = new SettingTypeText
                    {
                        Text = new ValueRegex
                        {
                            RegEx = OppoConstants.IpAddressRegex
                        }
                    },
                    Label = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["en"] = "Enter the IP address of the Oppo player (mandatory)" }
                },
                new Setting
                {
                    Id = OppoConstants.OppoModelKey,
                    Field = new SettingTypeDropdown
                    {
                        Dropdown = new SettingTypeDropdownInner
                        {
                            Items = [
                                new SettingTypeDropdownItem
                                {
                                    Label = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["en"] = GetOppoModelName(OppoModel.BDP83) },
                                    Value = nameof(OppoModel.BDP83)
                                },
                                new SettingTypeDropdownItem
                                {
                                    Label = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["en"] = GetOppoModelName(OppoModel.BDP9X) },
                                    Value = nameof(OppoModel.BDP9X)
                                },
                                new SettingTypeDropdownItem
                                {
                                    Label = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["en"] = GetOppoModelName(OppoModel.BDP10X) },
                                    Value = nameof(OppoModel.BDP10X)
                                },
                                new SettingTypeDropdownItem
                                {
                                    Label = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["en"] = GetOppoModelName(OppoModel.UDP203) },
                                    Value = nameof(OppoModel.UDP203)
                                },
                                new SettingTypeDropdownItem
                                {
                                    Label = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["en"] = GetOppoModelName(OppoModel.UDP205) },
                                    Value = nameof(OppoModel.UDP205)
                                }
                            ],
                            Value = configurationItem?.Model.ToStringFast()
                        }
                    },
                    Label = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["en"] = "Select the model of your Oppo player (mandatory)" }
                },
                new Setting
                {
                    Id = OppoConstants.UseMediaEventsKey,
                    Field = new SettingTypeCheckbox
                    {
                        Checkbox = new SettingTypeCheckboxInner
                        {
                            Value = configurationItem?.UseMediaEvents ?? false
                        }
                    },
                    Label = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["en"] = "Use Media Events? This enables playback information at the expense of updates every second" }
                },
                new Setting
                {
                    Id = OppoConstants.ChapterOrMovieLengthKey,
                    Field = new SettingTypeDropdown
                    {
                        Dropdown = new SettingTypeDropdownInner
                        {
                            Value = configurationItem?.UseChapterLengthForMovies == true ? OppoConstants.ChapterLengthValue : OppoConstants.MovieLengthValue,
                            Items = [
                                new SettingTypeDropdownItem
                                {
                                    Label = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["en"] = "Chapter Length" },
                                    Value = OppoConstants.ChapterLengthValue
                                },
                                new SettingTypeDropdownItem
                                {
                                    Label = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["en"] = "Movie Length" },
                                    Value = OppoConstants.MovieLengthValue
                                }
                            ]
                        }
                    },
                    Label = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["en"] = "Use chapter or movie length for progress bar (only applicable if Media Events is enabled)?" }
                },
                new Setting
                {
                    Id = OppoConstants.MaxMessageHandlingWaitTimeInSecondsKey,
                    Field = new SettingTypeNumber
                    {
                        Number = new SettingTypeNumberInner
                        {
                            Value = maxMessageHandlingWaitTimeInSeconds,
                            Min = 0.1,
                            Max = 9.5,
                            Decimals = 1
                        }
                    },
                    Label = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["en"] = "Enter the max wait time for a message to be processed (global setting)" }
                }
            ]
        };

    protected override async ValueTask<SetupDriverUserDataResult> HandleEntityReconfigured(System.Net.WebSockets.WebSocket socket,
        SetDriverUserDataMsg payload,
        string wsId,
        OppoConfigurationItem configurationItem,
        CancellationToken cancellationToken)
    {
        var oppoModel = GetOppoModel(payload.MsgData.InputValues!);
        var useMediaEvents = payload.MsgData.InputValues!.TryGetValue(OppoConstants.UseMediaEventsKey, out var useMediaEventsValue) &&
                               useMediaEventsValue.Equals(bool.TrueString, StringComparison.OrdinalIgnoreCase);

        var useChapterLengthForMovies = payload.MsgData.InputValues!.TryGetValue(OppoConstants.ChapterOrMovieLengthKey, out var chapterOrMovieLengthValue) &&
                                        chapterOrMovieLengthValue.Equals(OppoConstants.ChapterLengthValue, StringComparison.OrdinalIgnoreCase);

        var newConfigurationItem = configurationItem with
        {
            Model = oppoModel,
            UseMediaEvents = useMediaEvents,
            UseChapterLengthForMovies = useChapterLengthForMovies
        };
        var configuration = await _configurationService.GetConfigurationAsync(cancellationToken);
        configuration.Entities.Remove(configurationItem);
        configuration.Entities.Add(newConfigurationItem);
        await _configurationService.UpdateConfigurationAsync(configuration, cancellationToken);

        return await GetSetupResultForClient(wsId, newConfigurationItem.EntityId, cancellationToken);
    }

    private static OppoModel GetOppoModel(Dictionary<string, string> msgDataSetupData) =>
        msgDataSetupData.TryGetValue(OppoConstants.OppoModelKey, out var oppoModel)
            ? oppoModel switch
            {
                _ when oppoModel.Equals(nameof(OppoModel.BDP83), StringComparison.OrdinalIgnoreCase) => OppoModel.BDP83,
                _ when oppoModel.Equals(nameof(OppoModel.BDP9X), StringComparison.OrdinalIgnoreCase) => OppoModel.BDP9X,
                _ when oppoModel.Equals(nameof(OppoModel.BDP10X), StringComparison.OrdinalIgnoreCase) => OppoModel.BDP10X,
                _ when oppoModel.Equals(nameof(OppoModel.UDP203), StringComparison.OrdinalIgnoreCase) => OppoModel.UDP203,
                _ => OppoModel.UDP205
            }
            : OppoModel.UDP203;

    private async ValueTask<SetupDriverUserDataResult> GetSetupResultForClient(string wsId, string entityId, CancellationToken cancellationToken) =>
        await TryGetOppoClientHolderAsync(wsId, entityId, IdentifierType.EntityId, cancellationToken) is { } oppoClientHolder
        && await oppoClientHolder.Client.IsConnectedAsync()
            ? SetupDriverUserDataResult.Finalized
            : SetupDriverUserDataResult.Error;

    private static string GetOppoModelName(in OppoModel oppoModel) =>
        oppoModel switch
        {
            OppoModel.BDP83 => "BDP-83",
            OppoModel.BDP9X => "BDP-9X",
            OppoModel.BDP10X => "BDP-10X",
            OppoModel.UDP203 => "UDP-203",
            _ => "UDP-205"
        };

    protected override async ValueTask<SetupDriverUserDataResult> HandleCreateNewEntity(System.Net.WebSockets.WebSocket socket, SetDriverUserDataMsg payload, string wsId, CancellationToken cancellationToken)
    {
        var configuration = await _configurationService.GetConfigurationAsync(cancellationToken);
        var driverMetadata = await _configurationService.GetDriverMetadataAsync(cancellationToken);
        var host = payload.MsgData.InputValues![OppoConstants.IpAddressKey];
        var oppoModel = GetOppoModel(payload.MsgData.InputValues!);
        var entityName = payload.MsgData.InputValues!.GetValueOrNull(OppoConstants.EntityName, $"{driverMetadata.Name["en"]} ({GetOppoModelName(oppoModel)}) - {host}");
        var deviceId = payload.MsgData.InputValues!.GetValueOrNull(OppoConstants.DeviceIdKey, host);
        bool? useMediaEvents = payload.MsgData.InputValues!.TryGetValue(OppoConstants.UseMediaEventsKey, out var useMediaEventsValue)
            ? useMediaEventsValue.Equals(bool.TrueString, StringComparison.OrdinalIgnoreCase)
            : null;

        bool? useChapterLengthForMovies = payload.MsgData.InputValues!.TryGetValue(OppoConstants.ChapterOrMovieLengthKey, out var chapterOrMovieLengthValue)
                                        ? chapterOrMovieLengthValue.Equals(OppoConstants.ChapterLengthValue, StringComparison.OrdinalIgnoreCase)
                                        : null;

        var entity = configuration.Entities.FirstOrDefault(x => x.EntityId.Equals(host, StringComparison.OrdinalIgnoreCase));
        if (entity is null)
        {
            _logger.AddingConfiguration(host);
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
            _logger.UpdatingConfiguration(deviceId);
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

        return await GetSetupResultForClient(wsId, entity.EntityId, cancellationToken);
    }

    protected override MediaPlayerEntityCommandMsgData<OppoCommandId>? DeserializeMediaPlayerCommandPayload(JsonDocument jsonDocument)
        => jsonDocument.Deserialize(OppoJsonSerializerContext.Instance.MediaPlayerEntityCommandMsgDataOppoCommandId);

    protected override ValueTask OnConnectAsync(ConnectEvent payload, string wsId, CancellationToken cancellationToken) => ValueTask.CompletedTask;

    protected override ValueTask<bool> OnDisconnectAsync(DisconnectEvent payload, string wsId, CancellationToken cancellationToken)
        => TryDisconnectOppoClientsAsync(wsId, payload.MsgData?.DeviceId, cancellationToken);

    protected override ValueTask OnAbortDriverSetupAsync(AbortDriverSetupEvent payload, string wsId, CancellationToken cancellationToken)
        => ValueTask.CompletedTask;

    protected override ValueTask OnEnterStandbyAsync(EnterStandbyEvent payload, string wsId, CancellationToken cancellationToken)
    {
        _oppoClientFactory.TryDisposeAllClients();
        return ValueTask.CompletedTask;
    }

    protected override ValueTask OnExitStandbyAsync(ExitStandbyEvent payload, string wsId, CancellationToken cancellationToken)
        => ValueTask.CompletedTask;

    protected override async ValueTask<bool> IsEntityReachableAsync(string wsId, string entityId, CancellationToken cancellationToken) =>
        await TryGetOppoClientHolderAsync(wsId, entityId, IdentifierType.EntityId, cancellationToken) is { } oppoClientHolder
        && await oppoClientHolder.Client.IsConnectedAsync();
}