using System.ComponentModel.DataAnnotations;
using Theodicean.SourceGenerators;

namespace UnfoldedCircle.Models.Sync;

[JsonConverter(typeof(MediaPlayerEntityFeatureJsonConverter))]
public enum MediaPlayerEntityFeature
{
    [Display(Name = "on_off")]
    OnOff = 1,
    
    [Display(Name = "toggle")]
    Toggle,
    
    [Display(Name = "volume")]
    Volume,
    
    [Display(Name = "volume_up_down")]
    VolumeUpDown,
    
    [Display(Name = "mute_toggle")]
    MuteToggle,
    
    [Display(Name = "mute")]
    Mute,
    
    [Display(Name = "unmute")]
    Unmute,
    
    [Display(Name = "play_pause")]
    PlayPause,
    
    [Display(Name = "stop")]
    Stop,
    
    [Display(Name = "next")]
    Next,
    
    [Display(Name = "previous")]
    Previous,
    
    [Display(Name = "fast_forward")]
    FastForward,
    
    [Display(Name = "rewind")]
    Rewind,
    
    [Display(Name = "repeat")]
    Repeat,
    
    [Display(Name = "shuffle")]
    Shuffle,
    
    [Display(Name = "seek")]
    Seek,
    
    [Display(Name = "media_duration")]
    MediaDuration,
    
    [Display(Name = "media_position")]
    MediaPosition,
    
    [Display(Name = "media_title")]
    MediaTitle,
    
    [Display(Name = "media_artist")]
    MediaArtist,
    
    [Display(Name = "media_album")]
    MediaAlbum,
    
    [Display(Name = "media_image_url")]
    MediaImageUrl,
    
    [Display(Name = "media_type")]
    MediaType,
    
    [Display(Name = "dpad")]
    Dpad,
    
    [Display(Name = "numpad")]
    Numpad,
    
    [Display(Name = "home")]
    Home,
    
    [Display(Name = "menu")]
    Menu,
    
    [Display(Name = "context_menu")]
    ContextMenu,
    
    [Display(Name = "guide")]
    Guide,
    
    [Display(Name = "info")]
    Info,
    
    [Display(Name = "color_buttons")]
    ColorButtons,
    
    [Display(Name = "channel_switcher")]
    ChannelSwitcher,
    
    [Display(Name = "select_source")]
    SelectSource,
    
    [Display(Name = "select_sound_mode")]
    SelectSoundMode,
    
    [Display(Name = "eject")]
    Eject,
    
    [Display(Name = "open_close")]
    OpenClose,
    
    [Display(Name = "audio_track")]
    AudioTrack,
    
    [Display(Name = "subtitle")]
    Subtitle,
    
    [Display(Name = "record")]
    Record,
    
    [Display(Name = "settings")]
    Settings
}

[EnumJsonConverter(typeof(MediaPlayerEntityFeature), CaseSensitive = false, PropertyName = "features")]
public partial class MediaPlayerEntityFeatureJsonConverter;