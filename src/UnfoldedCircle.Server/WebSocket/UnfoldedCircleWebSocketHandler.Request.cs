using System.Collections.Concurrent;

using Oppo;

using UnfoldedCircle.Models.Events;
using UnfoldedCircle.Models.Shared;
using UnfoldedCircle.Models.Sync;
using UnfoldedCircle.Server.Configuration;
using UnfoldedCircle.Server.Event;
using UnfoldedCircle.Server.Json;
using UnfoldedCircle.Server.Oppo;
using UnfoldedCircle.Server.Response;

namespace UnfoldedCircle.Server.WebSocket;

internal sealed partial class UnfoldedCircleWebSocketHandler
{
    private static readonly ConcurrentDictionary<string, string> SocketIdEntityIpMap = new(StringComparer.Ordinal);
    private static readonly ConcurrentDictionary<string, bool> SubscribeEvents = new(StringComparer.Ordinal);
    
    private async Task HandleRequestMessage(
        System.Net.WebSockets.WebSocket socket,
        string wsId,
        MessageEvent messageEvent,
        JsonDocument jsonDocument,
        CancellationTokenWrapper cancellationTokenWrapper)
    {
        switch (messageEvent)
        {
            case MessageEvent.GetDriverVersion:
            {
                var payload = jsonDocument.Deserialize(UnfoldedCircleJsonSerializerContext.Instance.CommonReq)!;
                var driverMetadata = await _configurationService.GetDriverMetadataAsync(cancellationTokenWrapper.RequestAborted);
                await SendAsync(socket,
                    ResponsePayloadHelpers.CreateDriverVersionResponsePayload(
                        payload,
                        new DriverVersion
                        {
                            Name = driverMetadata.Name["en"],
                            Version = new DriverVersionInner
                            {
                                Driver = driverMetadata.Version
                            }
                        }),
                    wsId,
                    cancellationTokenWrapper.RequestAborted);
                
                return;
            }
            case MessageEvent.GetDriverMetaData:
            {
                var payload = jsonDocument.Deserialize(UnfoldedCircleJsonSerializerContext.Instance.CommonReq)!;
                
                await SendAsync(socket,
                    ResponsePayloadHelpers.CreateDriverMetaDataResponsePayload(payload, await _configurationService.GetDriverMetadataAsync(cancellationTokenWrapper.RequestAborted)),
                    wsId,
                    cancellationTokenWrapper.RequestAborted);
                
                return;
            }
            case MessageEvent.GetDeviceState:
            {
                var payload = jsonDocument.Deserialize(UnfoldedCircleJsonSerializerContext.Instance.GetDeviceStateMsg)!;
                var oppoClientHolder = await TryGetOppoClientHolder(wsId, payload.MsgData.DeviceId, IdentifierType.DeviceId, cancellationTokenWrapper.RequestAborted);
                await SendAsync(socket,
                    ResponsePayloadHelpers.CreateGetDeviceStateResponsePayload(
                        await GetDeviceState(oppoClientHolder),
                        payload.MsgData.DeviceId
                    ),
                    wsId,
                    cancellationTokenWrapper.RequestAborted);

                return;
            }
            case MessageEvent.GetAvailableEntities:
            {
                var payload = jsonDocument.Deserialize(UnfoldedCircleJsonSerializerContext.Instance.GetAvailableEntitiesMsg)!;
                var entities = await GetEntities(wsId, payload.MsgData.Filter?.DeviceId, cancellationTokenWrapper.RequestAborted);
                await SendAsync(socket,
                    ResponsePayloadHelpers.CreateGetAvailableEntitiesMsg(payload,
                        new AvailableEntitiesMsgData
                        {
                            Filter = payload.MsgData.Filter,
                            AvailableEntities = GetAvailableEntities(entities, payload).ToArray()
                        }),
                    wsId,
                    cancellationTokenWrapper.RequestAborted);

                return;
            }
            case MessageEvent.SubscribeEvents:
            {
                var payload = jsonDocument.Deserialize(UnfoldedCircleJsonSerializerContext.Instance.CommonReq)!;
                await SendAsync(socket,
                    ResponsePayloadHelpers.CreateCommonResponsePayload(payload),
                    wsId,
                    cancellationTokenWrapper.RequestAborted);

                SubscribeEvents.AddOrUpdate(wsId, static _ => true, static (_, _) => true);
                var oppoClientHolders = await TryGetOppoClientHolders(wsId, cancellationTokenWrapper.RequestAborted);
                if (oppoClientHolders is { Count: > 0 })
                {
                    foreach (var oppoClientHolder in oppoClientHolders)
                    {
                        if (await oppoClientHolder.Client.IsConnectedAsync())
                            _ = HandleEventUpdates(socket, wsId, oppoClientHolder, cancellationTokenWrapper);

                        if (oppoClientHolder.ClientKey.Model is not OppoModel.BDP8395
                            && await oppoClientHolder.Client.IsConnectedAsync())
                        {
                            await SendAsync(socket,
                                ResponsePayloadHelpers.CreateStateChangedResponsePayload(
                                    new MediaPlayerStateChangedEventMessageDataAttributes
                                    {
                                        SourceList = OppoEntitySettings.SourceList[oppoClientHolder.ClientKey.Model]
                                    },
                                    oppoClientHolder.ClientKey.HostName,
                                    EntityType.MediaPlayer),
                                wsId,
                                cancellationTokenWrapper.RequestAborted);
                        }
                    }
                }
                
                return;
            }
            case MessageEvent.UnsubscribeEvents:
            {
                var payload = jsonDocument.Deserialize(UnfoldedCircleJsonSerializerContext.Instance.UnsubscribeEventsMsg)!;
                
                await RemoveConfiguration(new RemoveInstruction(payload.MsgData?.DeviceId.GetUnprefixedIdentifier(), payload.MsgData?.EntityIds?.Select(static x => x.GetUnprefixedIdentifier()!), null), cancellationTokenWrapper.ApplicationStopping);
                await SendAsync(socket,
                    ResponsePayloadHelpers.CreateCommonResponsePayload(payload),
                    wsId,
                    cancellationTokenWrapper.RequestAborted);
                
                return;
            }
            case MessageEvent.GetEntityStates:
            {
                var payload = jsonDocument.Deserialize(UnfoldedCircleJsonSerializerContext.Instance.GetEntityStatesMsg)!;
                var entities = await GetEntities(wsId, payload.MsgData?.DeviceId, cancellationTokenWrapper.RequestAborted);
                await SendAsync(socket,
                    ResponsePayloadHelpers.CreateGetEntityStatesResponsePayload(payload,
                        entities is { Count: > 0 }
                            ? entities.Select(static x => new EntityIdDeviceId(x.EntityId, x.DeviceId, x.Model))
                            : []),
                    wsId,
                    cancellationTokenWrapper.RequestAborted);
                
                return;
            }
            case MessageEvent.SetupDriver:
            {
                var payload = jsonDocument.Deserialize(UnfoldedCircleJsonSerializerContext.Instance.SetupDriverMsg)!;
                SocketIdEntityIpMap.AddOrUpdate(wsId,
                    static (_, arg) => arg.MsgData.SetupData[OppoConstants.IpAddressKey],
                    static (_, _, arg) => arg.MsgData.SetupData[OppoConstants.IpAddressKey], payload);
                SubscribeEvents.AddOrUpdate(wsId, static _ => false, static (_, _) => false);
                
                var entity = await UpdateConfiguration(payload.MsgData.SetupData, cancellationTokenWrapper.ApplicationStopping);
                var oppoClientHolder = await TryGetOppoClientHolder(wsId, entity.EntityId, IdentifierType.EntityId, cancellationTokenWrapper.RequestAborted);
                
                var isConnected = oppoClientHolder is not null && await oppoClientHolder.Client.IsConnectedAsync();
                
                await Task.WhenAll(
                    SendAsync(socket,
                        ResponsePayloadHelpers.CreateCommonResponsePayload(payload),
                        wsId,
                        cancellationTokenWrapper.RequestAborted),
                    SendAsync(socket,
                        ResponsePayloadHelpers.CreateDeviceSetupChangeResponsePayload(isConnected),
                        wsId,
                        cancellationTokenWrapper.RequestAborted),
                    SendAsync(socket,
                        ResponsePayloadHelpers.CreateConnectEventResponsePayload(await GetDeviceState(oppoClientHolder)),
                        wsId,
                        cancellationTokenWrapper.RequestAborted)
                );
                
                return;
            }
            case MessageEvent.SetupDriverUserData:
            {
                var payload = jsonDocument.Deserialize(UnfoldedCircleJsonSerializerContext.Instance.SetDriverUserDataMsg)!;
                SocketIdEntityIpMap.AddOrUpdate(wsId,
                    static (_, arg) => arg.MsgData.SetupData![OppoConstants.IpAddressKey],
                    static (_, _, arg) => arg.MsgData.SetupData![OppoConstants.IpAddressKey], payload);
                await SendAsync(socket,
                    ResponsePayloadHelpers.CreateCommonResponsePayload(payload),
                    wsId,
                    cancellationTokenWrapper.RequestAborted);
                
                return;
            }
            case MessageEvent.EntityCommand:
            {
                var entityType = GetEntityType(jsonDocument);
                if (entityType == EntityType.MediaPlayer)
                {
                    var payload = jsonDocument.Deserialize(UnfoldedCircleJsonSerializerContext.Instance.MediaPlayerEntityCommandMsgDataOppoCommandId)!;
                    await HandleEntityCommand(socket, payload, wsId, cancellationTokenWrapper);
                }
                else if (entityType == EntityType.Remote)
                {
                    var payload = jsonDocument.Deserialize(UnfoldedCircleJsonSerializerContext.Instance.RemoteEntityCommandMsgData)!;
                    await HandleEntityCommand(socket, payload, wsId, cancellationTokenWrapper);
                }

                return;
            }
            case MessageEvent.SupportedEntityTypes:
            default:
                return;
        }
    }

    private static EntityType? GetEntityType(JsonDocument jsonDocument)
    {
        return jsonDocument.RootElement.TryGetProperty("msg_data", out var msgDataElement) && msgDataElement.TryGetProperty("entity_type", out var value)
            ? value.Deserialize(UnfoldedCircleJsonSerializerContext.Instance.EntityType)
            : null;
    }

    private static IEnumerable<AvailableEntity> GetAvailableEntities(
        List<UnfoldedCircleConfigurationItem>? entities,
        GetAvailableEntitiesMsg payload)
    {
        if (entities is not { Count: > 0 })
            yield break;

        var hasDeviceIdFilter = !string.IsNullOrEmpty(payload.MsgData.Filter?.DeviceId);
        var hasEntityTypeFilter = payload.MsgData.Filter?.EntityType is not null;
        foreach (var unfoldedCircleConfigurationItem in entities)
        {
            if (hasDeviceIdFilter && !string.Equals(unfoldedCircleConfigurationItem.DeviceId, payload.MsgData.Filter!.DeviceId.GetUnprefixedIdentifier(), StringComparison.OrdinalIgnoreCase))
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

    private static MediaPlayerAvailableEntity GetMediaPlayerEntity(UnfoldedCircleConfigurationItem configurationItem) =>
        new()
        {
            EntityId = configurationItem.EntityId.GetIdentifier(EntityType.MediaPlayer),
            EntityType = EntityType.MediaPlayer,
            Name = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["en"] = configurationItem.DeviceName },
            DeviceId = configurationItem.DeviceId.GetNullableIdentifier(EntityType.MediaPlayer),
            Features = OppoEntitySettings.MediaPlayerEntityFeatures,
            Options = new Dictionary<string, ISet<string>>(StringComparer.OrdinalIgnoreCase) { ["simple_commands"] = OppoEntitySettings.MediaPlayerSimpleCommands }
        };

    private static RemoteAvailableEntity GetRemoteEntity(UnfoldedCircleConfigurationItem configurationItem) =>
        new()
        {
            EntityId = configurationItem.EntityId.GetIdentifier(EntityType.Remote),
            EntityType = EntityType.Remote,
            Name = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["en"] = $"{configurationItem.DeviceName} Remote" },
            DeviceId = configurationItem.DeviceId.GetNullableIdentifier(EntityType.Remote),
            Features = OppoEntitySettings.RemoteFeatures,
            Options = OppoEntitySettings.RemoteOptions
        };
}