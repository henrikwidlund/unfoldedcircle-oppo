using Oppo;

using UnfoldedCircle.Models.Events;
using UnfoldedCircle.Models.Shared;
using UnfoldedCircle.Models.Sync;
using UnfoldedCircle.Server.Configuration;
using UnfoldedCircle.Server.Json;

namespace UnfoldedCircle.Server.Response;

internal static class ResponsePayloadHelpers
{
    const string EventKind = "event";
    
    private static byte[]? _createAuthResponsePayload;
    internal static byte[] CreateAuthResponsePayload() =>
        _createAuthResponsePayload ??= JsonSerializer.SerializeToUtf8Bytes(new AuthMsg
            {
                Kind = "resp",
                ReqId = 0,
                Msg = "authentication",
                Code = 200
            },
            UnfoldedCircleJsonSerializerContext.Instance.AuthMsg);

    internal static byte[] CreateDriverVersionResponsePayload(
        CommonReq req,
        DriverVersion driverVersionResponseData) =>
        JsonSerializer.SerializeToUtf8Bytes(new DriverVersionMsg
            {
                Kind = "resp",
                ReqId = req.Id,
                Msg = "get_driver_version",
                Code = 200,
                MsgData = driverVersionResponseData
            },
            UnfoldedCircleJsonSerializerContext.Instance.DriverVersionMsg);

    internal static byte[] CreateDriverMetaDataResponsePayload(
        CommonReq req,
        DriverMetadata driverMetadata) =>
        JsonSerializer.SerializeToUtf8Bytes(new DriverMetadataMsg
            {
                Kind = "resp",
                ReqId = req.Id,
                Msg = "driver_metadata",
                Code = 200,
                MsgData = driverMetadata
            },
            UnfoldedCircleJsonSerializerContext.Instance.DriverMetadataMsg);

    internal static byte[] CreateGetDeviceStateResponsePayload(
        in DeviceState deviceState,
        string? deviceId) =>
        JsonSerializer.SerializeToUtf8Bytes(new DeviceStateEventMsg
        {
            Kind = EventKind,
            Msg = "device_state",
            Cat = "DEVICE",
            TimeStamp = DateTime.UtcNow,
            MsgData = new DeviceStateItem
            {
                State = deviceState,
                DeviceId = deviceId
            }
        }, UnfoldedCircleJsonSerializerContext.Instance.DeviceStateEventMsg);

    internal static byte[] CreateGetAvailableEntitiesMsg<TFeature>(
        GetAvailableEntitiesMsg req,
        AvailableEntitiesMsgData<TFeature> availableEntitiesMsgData)
        where TFeature : struct, Enum =>
        JsonSerializer.SerializeToUtf8Bytes(new AvailableEntitiesMsg<TFeature>
            {
                Kind = "resp",
                ReqId = req.Id,
                Msg = "available_entities",
                Code = 200,
                MsgData = availableEntitiesMsgData
            },
            UnfoldedCircleJsonSerializerContext.Instance.AvailableEntitiesMsgMediaPlayerEntityFeature);

    public static byte[] CreateCommonResponsePayload(
        CommonReq req) =>
        CreateCommonResponsePayload(req.Id);

    public static byte[] CreateCommonResponsePayload(
        in uint requestId) =>
        JsonSerializer.SerializeToUtf8Bytes(new CommonResp
            {
                Code = 200,
                Kind = "resp",
                ReqId = requestId,
                Msg = "result"
            },
            UnfoldedCircleJsonSerializerContext.Instance.CommonResp);

    public static byte[] CreateGetEntityStatesResponsePayload(
        CommonReq req,
        IEnumerable<EntityIdDeviceId> entityIdDeviceIds) =>
        JsonSerializer.SerializeToUtf8Bytes(new EntityStates<MediaPlayerEntityAttribute>
        {
            Code = 200,
            Kind = "resp",
            ReqId = req.Id,
            Msg = "entity_states",
            MsgData = entityIdDeviceIds.Select(static x => new EntityStateChanged<MediaPlayerEntityAttribute>
            {
                EntityId = x.EntityId,
                EntityType = EntityType.MediaPlayer,
                Attributes = GetMediaPlayerAttributes(x.Model),
                DeviceId = x.DeviceId
            }).ToArray()
        }, UnfoldedCircleJsonSerializerContext.Instance.EntityStatesMediaPlayerEntityAttribute);

    private static MediaPlayerEntityAttribute[] GetMediaPlayerAttributes(in OppoModel model)
    {
        return model switch
        {
            OppoModel.BDP8395 =>
            [
                MediaPlayerEntityAttribute.State,
                MediaPlayerEntityAttribute.Volume,
                MediaPlayerEntityAttribute.Muted,
                MediaPlayerEntityAttribute.MediaPosition,
                MediaPlayerEntityAttribute.MediaDuration,
                MediaPlayerEntityAttribute.MediaType,
                MediaPlayerEntityAttribute.Repeat,
                MediaPlayerEntityAttribute.Shuffle
            ],
            OppoModel.BDP10X =>
            [
                MediaPlayerEntityAttribute.State,
                MediaPlayerEntityAttribute.Volume,
                MediaPlayerEntityAttribute.Muted,
                MediaPlayerEntityAttribute.MediaPosition,
                MediaPlayerEntityAttribute.MediaDuration,
                MediaPlayerEntityAttribute.MediaType,
                MediaPlayerEntityAttribute.Repeat,
                MediaPlayerEntityAttribute.Shuffle,
                MediaPlayerEntityAttribute.Source,
                MediaPlayerEntityAttribute.SourceList
            ],
            OppoModel.UDP203 or OppoModel.UDP205 =>
            [
                MediaPlayerEntityAttribute.State,
                MediaPlayerEntityAttribute.Volume,
                MediaPlayerEntityAttribute.Muted,
                MediaPlayerEntityAttribute.MediaPosition,
                MediaPlayerEntityAttribute.MediaDuration,
                MediaPlayerEntityAttribute.MediaTitle,
                MediaPlayerEntityAttribute.MediaArtist,
                MediaPlayerEntityAttribute.MediaAlbum,
                MediaPlayerEntityAttribute.MediaImageUrl,
                MediaPlayerEntityAttribute.MediaType,
                MediaPlayerEntityAttribute.Repeat,
                MediaPlayerEntityAttribute.Shuffle,
                MediaPlayerEntityAttribute.Source,
                MediaPlayerEntityAttribute.SourceList
            ],
            _ => throw new ArgumentOutOfRangeException(nameof(model), model, null)
        };
    }
    
    public static byte[] CreateDeviceSetupChangeResponsePayload(
        in bool isConnected) =>
        JsonSerializer.SerializeToUtf8Bytes(new DriverSetupChangeEvent
        {
            Kind = EventKind,
            Msg = "driver_setup_change",
            Cat = "DEVICE",
            TimeStamp = DateTime.UtcNow,
            MsgData = new DriverSetupChange
            {
                State = isConnected ? DriverSetupChangeState.Ok : DriverSetupChangeState.Error,
                EventType = DriverSetupChangeEventType.Stop,
                Error = isConnected ? null : DriverSetupChangeError.NotFound
            }
        }, UnfoldedCircleJsonSerializerContext.Instance.DriverSetupChangeEvent);

    public static byte[] CreateConnectEventResponsePayload(
        in DeviceState deviceState)
    {
        return JsonSerializer.SerializeToUtf8Bytes(new ConnectEventMsg
        {
            Kind = EventKind,
            Msg = "device_state",
            Cat = "DEVICE",
            TimeStamp = DateTime.UtcNow,
            MsgData = new ConnectDeviceStateItem { State = deviceState }
        }, UnfoldedCircleJsonSerializerContext.Instance.ConnectEventMsg);
    }

    internal static byte[] CreateValidationErrorResponsePayload(
        CommonReq req,
        ValidationError validationError) =>
        JsonSerializer.SerializeToUtf8Bytes(new CommonRespRequired<ValidationError>
        {
            Kind = "resp",
            ReqId = req.Id,
            Msg = "validation_error",
            Code = 400,
            MsgData = validationError
        }, UnfoldedCircleJsonSerializerContext.Instance.CommonRespRequiredValidationError);

    internal static byte[] CreateStateChangedResponsePayload(
        StateChangedEventMessageDataAttributes attributes,
        string entityId) =>
        JsonSerializer.SerializeToUtf8Bytes(new StateChangedEvent
        {
            Kind = EventKind,
            Msg = "entity_change",
            Cat = "ENTITY",
            TimeStamp = DateTime.UtcNow,
            MsgData = new StateChangedEventMessageData
            {
                EntityId = entityId,
                EntityType = EntityType.MediaPlayer,
                Attributes = attributes
            }
        }, UnfoldedCircleJsonSerializerContext.Instance.StateChangedEvent);
}