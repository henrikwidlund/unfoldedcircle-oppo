using UnfoldedCircle.Models.Events;
using UnfoldedCircle.Models.Shared;
using UnfoldedCircle.Models.Sync;
using UnfoldedCircle.Server.AlbumCover;
using UnfoldedCircle.Server.Configuration;

namespace UnfoldedCircle.Server.Json;

[JsonSerializable(typeof(CommonReq))]
[JsonSerializable(typeof(CommonResp))]
[JsonSerializable(typeof(ConnectEvent))]
[JsonSerializable(typeof(DisconnectEvent))]
[JsonSerializable(typeof(AuthMsg))]
[JsonSerializable(typeof(DriverVersionMsg))]
[JsonSerializable(typeof(DriverMetadataMsg))]
[JsonSerializable(typeof(DeviceStateEventMsg))]
[JsonSerializable(typeof(ConnectEventMsg))]
[JsonSerializable(typeof(GetAvailableEntitiesMsg))]
[JsonSerializable(typeof(AvailableEntitiesMsg))]
[JsonSerializable(typeof(SetupDriverMsg))]
[JsonSerializable(typeof(DriverSetupChangeEvent))]
[JsonSerializable(typeof(MediaPlayerEntityCommandMsgData<OppoCommandId>))]
[JsonSerializable(typeof(RemoteEntityCommandMsgData))]
[JsonSerializable(typeof(CommonRespRequired<ValidationError>))]
[JsonSerializable(typeof(GetDeviceStateMsg))]
[JsonSerializable(typeof(SetDriverUserDataMsg))]
[JsonSerializable(typeof(AbortDriverSetupEvent))]
[JsonSerializable(typeof(GetEntityStatesMsg))]
[JsonSerializable(typeof(EntityStates))]
[JsonSerializable(typeof(UnfoldedCircleConfiguration))]
[JsonSerializable(typeof(UnsubscribeEventsMsg))]
[JsonSerializable(typeof(EnterStandbyEvent))]
[JsonSerializable(typeof(ExitStandbyEvent))]
[JsonSerializable(typeof(StateChangedEvent<MediaPlayerStateChangedEventMessageDataAttributes>))]
[JsonSerializable(typeof(StateChangedEvent<RemoteStateChangedEventMessageDataAttributes>))]
[JsonSerializable(typeof(ArtistAlbumsResponse))]
[JsonSerializable(typeof(ArtistTrackResponse))]
[JsonSerializable(typeof(EntityType?))]
internal sealed partial class UnfoldedCircleJsonSerializerContext : JsonSerializerContext
{
    internal static readonly UnfoldedCircleJsonSerializerContext Instance = new(new JsonSerializerOptions
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new SettingTypeFieldConverter() }
    });

    internal static readonly UnfoldedCircleJsonSerializerContext InstanceWithoutCustomConverters = new(new JsonSerializerOptions
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    });
}