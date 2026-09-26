using UnfoldedCircle.Models.Sync;
using UnfoldedCircle.OppoBluRay.AlbumCover;
using UnfoldedCircle.OppoBluRay.Configuration;
using UnfoldedCircle.OppoBluRay.Metadata;
using UnfoldedCircle.OppoBluRay.OppoEntity;
using UnfoldedCircle.Server.Configuration;

namespace UnfoldedCircle.OppoBluRay.Json;

[JsonSerializable(typeof(MediaPlayerEntityCommandMsgData<OppoCommandId>))]
[JsonSerializable(typeof(ArtistAlbumsResponse))]
[JsonSerializable(typeof(ArtistTrackResponse))]
[JsonSerializable(typeof(GetMusicPlayInfoResponse))]
[JsonSerializable(typeof(UnfoldedCircleConfiguration<OppoGlobalConfiguration, OppoConfigurationItem>))]
internal sealed partial class OppoJsonSerializerContext : JsonSerializerContext
{
    internal static readonly OppoJsonSerializerContext Instance = new(new JsonSerializerOptions
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    });
}
