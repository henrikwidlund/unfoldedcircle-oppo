using OppoTelnet;

using UnfoldedCircle.Models.Events;
using UnfoldedCircle.Models.Shared;
using UnfoldedCircle.Models.Sync;
using UnfoldedCircle.Server.Json;
using UnfoldedCircle.Server.Oppo;

namespace UnfoldedCircle.Server.Response;

internal static class ResponsePayloadHelpers
{
    private static byte[]? _createAuthResponsePayload;
    internal static byte[] CreateAuthResponsePayload(UnfoldedCircleJsonSerializerContext jsonSerializerContext) =>
        _createAuthResponsePayload ??= JsonSerializer.SerializeToUtf8Bytes(new AuthMsg
            {
                Kind = "resp",
                ReqId = 0,
                Msg = "authentication",
                Code = 200
            },
            jsonSerializerContext.AuthMsg);

    internal static byte[] CreateDriverVersionResponsePayload(
        CommonReq req,
        DriverVersion driverVersionResponseData,
        UnfoldedCircleJsonSerializerContext jsonSerializerContext) =>
        JsonSerializer.SerializeToUtf8Bytes(new DriverVersionMsg
            {
                Kind = "resp",
                ReqId = req.Id,
                Msg = "get_driver_version",
                Code = 200,
                MsgData = driverVersionResponseData
            },
            jsonSerializerContext.DriverVersionMsg);

    internal static byte[] CreateDriverMetaDataResponsePayload(
        CommonReq req,
        DriverMetadata driverMetadata,
        UnfoldedCircleJsonSerializerContext jsonSerializerContext) =>
        JsonSerializer.SerializeToUtf8Bytes(new DriverMetadataMsg
            {
                Kind = "resp",
                ReqId = req.Id,
                Msg = "driver_metadata",
                Code = 200,
                MsgData = driverMetadata
            },
            jsonSerializerContext.DriverMetadataMsg);

    internal static byte[] CreateGetDeviceStateResponsePayload(
        in DeviceState deviceState,
        string? deviceId,
        UnfoldedCircleJsonSerializerContext jsonSerializerContext) =>
        JsonSerializer.SerializeToUtf8Bytes(new DeviceStateEventMsg
        {
            Kind = "event",
            Msg = "device_state",
            Cat = "DEVICE",
            TimeStamp = DateTime.UtcNow,
            MsgData = new DeviceStateItem
            {
                State = deviceState,
                DeviceId = deviceId
            }
        }, jsonSerializerContext.DeviceStateEventMsg);

    internal static byte[] CreateGetAvailableEntitiesMsg<TFeature>(
        GetAvailableEntitiesMsg req,
        AvailableEntitiesMsgData<TFeature> availableEntitiesMsgData,
        UnfoldedCircleJsonSerializerContext jsonSerializerContext)
        where TFeature : struct, Enum =>
        JsonSerializer.SerializeToUtf8Bytes(new AvailableEntitiesMsg<TFeature>
            {
                Kind = "resp",
                ReqId = req.Id,
                Msg = "available_entities",
                Code = 200,
                MsgData = availableEntitiesMsgData
            },
            jsonSerializerContext.AvailableEntitiesMsgMediaPlayerEntityFeature);

    public static byte[] CreateCommonResponsePayload(
        CommonReq req,
        UnfoldedCircleJsonSerializerContext jsonSerializerContext) =>
        CreateCommonResponsePayload(req.Id, jsonSerializerContext);

    public static byte[] CreateCommonResponsePayload(
        in uint requestId,
        UnfoldedCircleJsonSerializerContext jsonSerializerContext) =>
        JsonSerializer.SerializeToUtf8Bytes(new CommonResp
            {
                Code = 200,
                Kind = "resp",
                ReqId = requestId,
                Msg = "result"
            },
            jsonSerializerContext.CommonResp);

    public static byte[] CreateGetEntityStatesResponsePayload(
        CommonReq req,
        in bool isConnected,
        string? deviceId,
        UnfoldedCircleJsonSerializerContext jsonSerializerContext) =>
        JsonSerializer.SerializeToUtf8Bytes(new EntityStates<MediaPlayerEntityAttribute>
        {
            Code = 200,
            Kind = "resp",
            ReqId = req.Id,
            Msg = "entity_states",
            MsgData = isConnected
                ?
                [
                    new EntityStateChanged<MediaPlayerEntityAttribute>
                    {
                        EntityId = OppoConstants.EntityId,
                        EntityType = EntityType.MediaPlayer,
                        Attributes = [],
                        DeviceId = deviceId
                    }
                ]
                : []
        }, jsonSerializerContext.EntityStatesMediaPlayerEntityAttribute);

    public static byte[] CreateDeviceSetupChangeResponsePayload(
        in bool isConnected,
        UnfoldedCircleJsonSerializerContext jsonSerializerContext) =>
        JsonSerializer.SerializeToUtf8Bytes(new DriverSetupChangeEvent
        {
            Kind = "event",
            Msg = "driver_setup_change",
            Cat = "DEVICE",
            TimeStamp = DateTime.UtcNow,
            MsgData = new DriverSetupChange
            {
                State = isConnected ? DriverSetupChangeState.Ok : DriverSetupChangeState.Error,
                EventType = DriverSetupChangeEventType.Stop,
                Error = isConnected ? null : DriverSetupChangeError.NotFound
            }
        }, jsonSerializerContext.DriverSetupChangeEvent);

    public static byte[] CreateConnectEventResponsePayload(
        in DeviceState deviceState,
        UnfoldedCircleJsonSerializerContext jsonSerializerContext) =>
        JsonSerializer.SerializeToUtf8Bytes(new ConnectEventMsg
        {
            Kind = "event",
            Msg = "device_state",
            Cat = "DEVICE",
            TimeStamp = DateTime.UtcNow,
            MsgData = new ConnectDeviceStateItem
            {
                State = deviceState
            }
        }, jsonSerializerContext.ConnectEventMsg);

    internal static byte[] CreateValidationErrorResponsePayload(
        CommonReq req,
        ValidationError validationError,
        UnfoldedCircleJsonSerializerContext jsonSerializerContext) =>
        JsonSerializer.SerializeToUtf8Bytes(new CommonRespRequired<ValidationError>
        {
            Kind = "resp",
            ReqId = req.Id,
            Msg = "validation_error",
            Code = 400,
            MsgData = validationError
        }, jsonSerializerContext.CommonRespRequiredValidationError);

    internal static byte[] CreateStateChangedResponsePayload(UnfoldedCircleJsonSerializerContext jsonSerializerContext) =>
        JsonSerializer.SerializeToUtf8Bytes(new StateChangedEvent
        {
            Kind = "event",
            Msg = "entity_change",
            Cat = "ENTITY",
            TimeStamp = DateTime.UtcNow,
            MsgData = new StateChangedEventMessageData
            {
                EntityId = OppoConstants.EntityId,
                EntityType = EntityType.MediaPlayer,
                Attributes = new StateChangedEventMessageDataAttributes { State = State.Off }
            }
        }, jsonSerializerContext.StateChangedEvent);

    internal static DeviceState GetDeviceState(this IOppoClient oppoClient) =>
        oppoClient switch
        {
            { IsConnected: true } => DeviceState.Connected,
            { IsConnected: false } => DeviceState.Disconnected,
            _ => DeviceState.Error
        };
}