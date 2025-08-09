using System.ComponentModel.DataAnnotations;
using Theodicean.SourceGenerators;

namespace UnfoldedCircle.Models.Sync;

[JsonConverter(typeof(MediaPlayerEntityAttributeJsonConverter))]
public enum MediaPlayerEntityAttribute
{
    /// <summary>
    /// State of the media player, influenced by the play and power commands.
    /// </summary>
    [Display(Name = "state")]
    State = 1,
    
    /// <summary>
    /// Current volume level.
    /// </summary>
    [Display(Name = "volume")]
    Volume,
    
    /// <summary>
    /// Flag if the volume is muted.
    /// </summary>
    [Display(Name = "muted")]
    Muted,
    
    /// <summary>
    /// Media duration in seconds.
    /// </summary>
    [Display(Name = "media_duration")]
    MediaDuration,
    
    /// <summary>
    /// Current media position in seconds.
    /// </summary>
    [Display(Name = "media_position")]
    MediaPosition,
    
    /// <summary>
    /// The type of media being played.
    /// </summary>
    [Display(Name = "media_type")]
    MediaType,
    
    /// <summary>
    /// URL to retrieve the album art or an image representing what's being played.
    /// </summary>
    [Display(Name = "media_image_url")]
    MediaImageUrl,
    
    /// <summary>
    /// Currently playing media information.
    /// </summary>
    [Display(Name = "media_title")]
    MediaTitle,
    
    /// <summary>
    /// Currently playing media information.
    /// </summary>
    [Display(Name = "media_artist")]
    MediaArtist,
    
    /// <summary>
    /// Currently playing media information.
    /// </summary>
    [Display(Name = "media_album")]
    MediaAlbum,
    
    /// <summary>
    /// Current repeat mode.
    /// </summary>
    [Display(Name = "repeat")]
    Repeat,
    
    /// <summary>
    /// Shuffle mode on or off.
    /// </summary>
    [Display(Name = "shuffle")]
    Shuffle,
    
    /// <summary>
    /// Currently selected media or input source.
    /// </summary>
    [Display(Name = "source")]
    Source,
    
    /// <summary>
    /// Available media or input sources.
    /// </summary>
    [Display(Name = "source_list")]
    SourceList,
    
    /// <summary>
    /// Currently selected sound mode.
    /// </summary>
    [Display(Name = "sound_mode")]
    SoundMode,
    
    /// <summary>
    /// Available sound modes.
    /// </summary>
    [Display(Name = "sound_mode_list")]
    SoundModeList
}

[EnumJsonConverter(typeof(MediaPlayerEntityAttribute), CaseSensitive = false, PropertyName = "attributes")]
public partial class MediaPlayerEntityAttributeJsonConverter;