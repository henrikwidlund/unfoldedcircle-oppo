using Oppo;

using UnfoldedCircle.Models.Shared;
using UnfoldedCircle.Models.Sync;
using UnfoldedCircle.OppoBluRay.Configuration;
using UnfoldedCircle.Server.Extensions;

namespace UnfoldedCircle.OppoBluRay.Response;

internal static class OppoResponsePayloadHelpers
{
    internal static IEnumerable<EntityStateChanged> GetEntityStates(IEnumerable<EntityIdDeviceId> entityIdDeviceIds)
    {
        foreach (var entityIdDeviceId in entityIdDeviceIds)
        {
            yield return new MediaPlayerEntityStateChanged
            {
                EntityId = entityIdDeviceId.EntityId.GetIdentifier(EntityType.MediaPlayer),
                EntityType = EntityType.MediaPlayer,
                Attributes = GetMediaPlayerAttributes(entityIdDeviceId.Model),
                DeviceId = entityIdDeviceId.DeviceId.GetNullableIdentifier(EntityType.MediaPlayer)
            };
            yield return new RemoteEntityStateChanged
            {
                EntityId = entityIdDeviceId.EntityId.GetIdentifier(EntityType.Remote),
                EntityType = EntityType.Remote,
                Attributes = [RemoteEntityAttribute.State],
                DeviceId = entityIdDeviceId.DeviceId.GetNullableIdentifier(EntityType.Remote)
            };
        }
    }

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
}