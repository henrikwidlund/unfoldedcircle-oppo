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
                    cancellationTokenWrapper.ApplicationStopping);
                
                return;
            }
            case MessageEvent.GetDriverMetaData:
            {
                var payload = jsonDocument.Deserialize(UnfoldedCircleJsonSerializerContext.Instance.CommonReq)!;
                
                await SendAsync(socket,
                    ResponsePayloadHelpers.CreateDriverMetaDataResponsePayload(payload, await _configurationService.GetDriverMetadataAsync(cancellationTokenWrapper.RequestAborted)),
                    wsId,
                    cancellationTokenWrapper.ApplicationStopping);
                
                return;
            }
            case MessageEvent.GetDeviceState:
            {
                var payload = jsonDocument.Deserialize(UnfoldedCircleJsonSerializerContext.Instance.GetDeviceStateMsg)!;
                var oppoClientHolder = await TryGetOppoClientHolder(wsId, payload.MsgData.DeviceId, IdentifierType.DeviceId, cancellationTokenWrapper.ApplicationStopping);
                await SendAsync(socket,
                    ResponsePayloadHelpers.CreateGetDeviceStateResponsePayload(
                        await GetDeviceState(oppoClientHolder),
                        payload.MsgData.DeviceId
                    ),
                    wsId,
                    cancellationTokenWrapper.ApplicationStopping);

                return;
            }
            case MessageEvent.GetAvailableEntities:
            {
                var payload = jsonDocument.Deserialize(UnfoldedCircleJsonSerializerContext.Instance.GetAvailableEntitiesMsg)!;
                var entities = await GetEntities(wsId, payload.MsgData.Filter?.DeviceId, cancellationTokenWrapper.ApplicationStopping);
                await SendAsync(socket,
                    ResponsePayloadHelpers.CreateGetAvailableEntitiesMsg(payload,
                        new AvailableEntitiesMsgData<MediaPlayerEntityFeature>
                        {
                            Filter = payload.MsgData.Filter,
                            AvailableEntities = GetAvailableEntities(entities, payload)
                        }),
                    wsId,
                    cancellationTokenWrapper.ApplicationStopping);

                return;
            }
            case MessageEvent.SubscribeEvents:
            {
                var payload = jsonDocument.Deserialize(UnfoldedCircleJsonSerializerContext.Instance.CommonReq)!;
                await SendAsync(socket,
                    ResponsePayloadHelpers.CreateCommonResponsePayload(payload),
                    wsId,
                    cancellationTokenWrapper.ApplicationStopping);

                SubscribeEvents.AddOrUpdate(wsId, static _ => true, static (_, _) => true);
                var oppoClientHolders = await TryGetOppoClientHolders(wsId, cancellationTokenWrapper.ApplicationStopping);
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
                                    new StateChangedEventMessageDataAttributes
                                    {
                                        SourceList = OppoEntitySettings.SourceList[oppoClientHolder.ClientKey.Model]
                                    },
                                    oppoClientHolder.ClientKey.HostName),
                                wsId,
                                cancellationTokenWrapper.ApplicationStopping);
                        }
                    }
                }
                
                return;
            }
            case MessageEvent.UnsubscribeEvents:
            {
                var payload = jsonDocument.Deserialize(UnfoldedCircleJsonSerializerContext.Instance.UnsubscribeEventsMsg)!;
                
                await RemoveConfiguration(new RemoveInstruction(payload.MsgData?.DeviceId, payload.MsgData?.EntityIds, null), cancellationTokenWrapper.ApplicationStopping);
                await SendAsync(socket,
                    ResponsePayloadHelpers.CreateCommonResponsePayload(payload),
                    wsId,
                    cancellationTokenWrapper.ApplicationStopping);
                
                return;
            }
            case MessageEvent.GetEntityStates:
            {
                var payload = jsonDocument.Deserialize(UnfoldedCircleJsonSerializerContext.Instance.GetEntityStatesMsg)!;
                var entities = await GetEntities(wsId, payload.MsgData?.DeviceId, cancellationTokenWrapper.ApplicationStopping);
                await SendAsync(socket,
                    ResponsePayloadHelpers.CreateGetEntityStatesResponsePayload(payload,
                        entities is { Count: > 0 }
                            ? entities.Select(static x => new EntityIdDeviceId(x.EntityId, x.DeviceId, x.Model))
                            : []),
                    wsId,
                    cancellationTokenWrapper.ApplicationStopping);
                
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
                var oppoClientHolder = await TryGetOppoClientHolder(wsId, entity.EntityId, IdentifierType.EntityId, cancellationTokenWrapper.ApplicationStopping);
                
                var isConnected = oppoClientHolder is not null && await oppoClientHolder.Client.IsConnectedAsync();
                
                await Task.WhenAll(
                    SendAsync(socket,
                        ResponsePayloadHelpers.CreateCommonResponsePayload(payload),
                        wsId,
                        cancellationTokenWrapper.ApplicationStopping),
                    SendAsync(socket,
                        ResponsePayloadHelpers.CreateDeviceSetupChangeResponsePayload(isConnected),
                        wsId,
                        cancellationTokenWrapper.ApplicationStopping),
                    SendAsync(socket,
                        ResponsePayloadHelpers.CreateConnectEventResponsePayload(await GetDeviceState(oppoClientHolder)),
                        wsId,
                        cancellationTokenWrapper.ApplicationStopping)
                );
                
                return;
            }
            case MessageEvent.SetupDriverUserData:
            {
                var payload = jsonDocument.Deserialize(UnfoldedCircleJsonSerializerContext.Instance.SetDriverUserDataMsg)!;
                SocketIdEntityIpMap.AddOrUpdate(wsId,
                    static (_, arg) => arg.MsgData.SetupData[OppoConstants.IpAddressKey],
                    static (_, _, arg) => arg.MsgData.SetupData[OppoConstants.IpAddressKey], payload);
                await SendAsync(socket,
                    ResponsePayloadHelpers.CreateCommonResponsePayload(payload),
                    wsId,
                    cancellationTokenWrapper.ApplicationStopping);
                
                return;
            }
            case MessageEvent.EntityCommand:
            {
                var payload = jsonDocument.Deserialize(UnfoldedCircleJsonSerializerContext.Instance.EntityCommandMsgOppoCommandId)!;
                await HandleEntityCommand(socket, payload, wsId, payload.MsgData.EntityId, cancellationTokenWrapper);
                return;
            }
            case MessageEvent.SupportedEntityTypes:
            default:
                return;
        }
    }

    private static AvailableEntity<MediaPlayerEntityFeature>[] GetAvailableEntities(
        List<UnfoldedCircleConfigurationItem>? entities,
        GetAvailableEntitiesMsg payload) =>
        entities is { Count: > 0 }
            ? entities.Select(x => new AvailableEntity<MediaPlayerEntityFeature>
            {
                EntityId = x.EntityId,
                EntityType = EntityType.MediaPlayer,
                Name = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["en"] = x.DeviceName },
                DeviceId = payload.MsgData.Filter?.DeviceId ?? x.DeviceId,
                Features = OppoEntitySettings.MediaPlayerEntityFeatures,
                Options = new Dictionary<string, ISet<string>>(StringComparer.OrdinalIgnoreCase)
                {
                    ["simple_commands"] = OppoEntitySettings.SimpleCommands
                }
            }).ToArray()
            : [];
}