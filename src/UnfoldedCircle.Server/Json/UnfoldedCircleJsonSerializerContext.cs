using UnfoldedCircle.Models.Events;
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
[JsonSerializable(typeof(AvailableEntitiesMsg<MediaPlayerEntityFeature>))]
[JsonSerializable(typeof(SetupDriverMsg))]
[JsonSerializable(typeof(DriverSetupChangeEvent))]
[JsonSerializable(typeof(EntityCommandMsg<OppoCommandId>))]
[JsonSerializable(typeof(CommonRespRequired<ValidationError>))]
[JsonSerializable(typeof(GetDeviceStateMsg))]
[JsonSerializable(typeof(SetDriverUserDataMsg))]
[JsonSerializable(typeof(AbortDriverSetupEvent))]
[JsonSerializable(typeof(GetEntityStatesMsg))]
[JsonSerializable(typeof(EntityStates<MediaPlayerEntityAttribute>))]
[JsonSerializable(typeof(UnfoldedCircleConfiguration))]
[JsonSerializable(typeof(UnsubscribeEventsMsg))]
[JsonSerializable(typeof(EnterStandbyEvent))]
[JsonSerializable(typeof(ExitStandbyEvent))]
[JsonSerializable(typeof(StateChangedEvent))]
[JsonSerializable(typeof(ArtistAlbumsResponse))]
[JsonSerializable(typeof(ArtistTrackResponse))]
internal partial class UnfoldedCircleJsonSerializerContext : JsonSerializerContext;