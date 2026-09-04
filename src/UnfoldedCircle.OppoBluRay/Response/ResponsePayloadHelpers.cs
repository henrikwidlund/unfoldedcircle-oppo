using Oppo;

using UnfoldedCircle.Models.Shared;
using UnfoldedCircle.Models.Sync;
using UnfoldedCircle.OppoBluRay.Configuration;
using UnfoldedCircle.OppoBluRay.OppoEntity;
using UnfoldedCircle.Server.Extensions;

namespace UnfoldedCircle.OppoBluRay.Response;

internal static class OppoResponsePayloadHelpers
{
    internal static IEnumerable<EntityStateChanged> GetEntityStates(IEnumerable<EntityIdDeviceId> entityIdDeviceIds)
    {
        foreach (var entityIdDeviceId in entityIdDeviceIds)
        {
            yield return new RemoteEntityStateChanged
            {
                EntityId = entityIdDeviceId.EntityId.GetIdentifier(EntityType.Remote),
                EntityType = EntityType.Remote,
                Attributes = [RemoteEntityAttribute.State],
                DeviceId = entityIdDeviceId.DeviceId.GetNullableIdentifier(EntityType.Remote)
            };

            if (entityIdDeviceId.Model == OppoModel.Magnetar)
                continue;

            yield return new MediaPlayerEntityStateChanged
            {
                EntityId = entityIdDeviceId.EntityId.GetIdentifier(EntityType.MediaPlayer),
                EntityType = EntityType.MediaPlayer,
                Attributes = GetMediaPlayerAttributes(entityIdDeviceId.Model),
                DeviceId = entityIdDeviceId.DeviceId.GetNullableIdentifier(EntityType.MediaPlayer)
            };

            foreach (var oppoSensorType in SensorHelpers.GetOppoSensorTypes(entityIdDeviceId.Model))
            {
                var suffix = oppoSensorType.ToStringFast();
                yield return new SensorEntityStateChanged
                {
                    EntityId = entityIdDeviceId.EntityId.GetIdentifier(EntityType.Sensor, suffix),
                    EntityType = EntityType.Sensor,
                    Attributes = [SensorEntityAttribute.State, SensorEntityAttribute.Unit, SensorEntityAttribute.Value],
                    DeviceId = entityIdDeviceId.DeviceId.GetNullableIdentifier(EntityType.Sensor, suffix)
                };
            }

            if (OppoEntitySettings.SourceList[entityIdDeviceId.Model].Length > 0)
            {
                yield return new SelectEntityStateChanged
                {
                    EntityId = entityIdDeviceId.EntityId.GetIdentifier(EntityType.Select, OppoConstants.InputSourceSelectSuffix),
                    EntityType = EntityType.Select,
                    Attributes = [SelectEntityAttribute.State, SelectEntityAttribute.CurrentOption, SelectEntityAttribute.Options],
                    DeviceId = entityIdDeviceId.DeviceId.GetNullableIdentifier(EntityType.Select, OppoConstants.InputSourceSelectSuffix)
                };
            }
        }
    }

    private static MediaPlayerEntityAttribute[] GetMediaPlayerAttributes(OppoModel model) =>
        model switch
        {
            OppoModel.BDP83 or OppoModel.BDP9X =>
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
            OppoModel.Magnetar => [],
            _ => throw new ArgumentOutOfRangeException(nameof(model), model, null)
        };
}
