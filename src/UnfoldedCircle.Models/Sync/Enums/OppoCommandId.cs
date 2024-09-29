using System.ComponentModel.DataAnnotations;
using UnfoldedCircle.Generators;

namespace UnfoldedCircle.Models.Sync;

[JsonConverter(typeof(OppoCommandIdJsonConverter))]
public enum OppoCommandId
{
    /// <summary>
    /// Switch on media player.
    /// </summary>
    [Display(Name = "on")]
    On = 1,
    
    /// <summary>
    /// Switch off media player.
    /// </summary>
    [Display(Name = "off")]
    Off,
    
    /// <summary>
    /// Toggle the current power state, either from on -> off or from off -> on.
    /// </summary>
    [Display(Name = "toggle")]
    Toggle,
    
    /// <summary>
    /// Toggle play / pause.
    /// </summary>
    [Display(Name = "play_pause")]
    PlayPause,
    
    /// <summary>
    /// Stop playback.
    /// </summary>
    [Display(Name = "stop")]
    Stop,
    
    /// <summary>
    /// Go back to previous track.
    /// </summary>
    [Display(Name = "previous")]
    Previous,
    
    /// <summary>
    /// Skip to next track.
    /// </summary>
    [Display(Name = "next")]
    Next,
    
    /// <summary>
    /// Fast forward current track.
    /// </summary>
    [Display(Name = "fast_forward")]
    FastForward,
    
    /// <summary>
    /// Rewind current track.
    /// </summary>
    [Display(Name = "rewind")]
    Rewind,
    
    /// <summary>
    /// Seek to given position in current track. Position is given in seconds.
    /// </summary>
    /// <remarks>Parameters: media_position</remarks>
    [Display(Name = "seek")]
    Seek,
    
    /// <summary>
    /// Set volume to given level.
    /// </summary>
    /// <remarks>Parameters: volume</remarks>
    [Display(Name = "volume")]
    Volume,
    
    /// <summary>
    /// Increase volume.
    /// </summary>
    [Display(Name = "volume_up")]
    VolumeUp,
    
    /// <summary>
    /// Decrease volume.
    /// </summary>
    [Display(Name = "volume_down")]
    VolumeDown,
    
    /// <summary>
    /// Toggle mute state.
    /// </summary>
    [Display(Name = "mute_toggle")]
    MuteToggle,
    
    /// <summary>
    /// Mute volume.
    /// </summary>
    [Display(Name = "mute")]
    Mute,
    
    /// <summary>
    /// Unmute volume.
    /// </summary>
    [Display(Name = "unmute")]
    Unmute,
    
    /// <summary>
    /// Repeat track or playlist.
    /// </summary>
    /// <remarks>Parameters: repeat</remarks>
    [Display(Name = "repeat")]
    Repeat,
    
    /// <summary>
    /// Shuffle playlist or start random playback.
    /// </summary>
    /// <remarks>Parameters: shuffle</remarks>
    [Display(Name = "shuffle")]
    Shuffle,
    
    /// <summary>
    /// Channel up.
    /// </summary>
    [Display(Name = "channel_up")]
    ChannelUp,
    
    /// <summary>
    /// Channel down.
    /// </summary>
    [Display(Name = "channel_down")]
    ChannelDown,
    
    /// <summary>
    /// Directional pad up.
    /// </summary>
    [Display(Name = "cursor_up")]
    CursorUp,
    
    /// <summary>
    /// Directional pad down.
    /// </summary>
    [Display(Name = "cursor_down")]
    CursorDown,
    
    /// <summary>
    /// Directional pad left.
    /// </summary>
    [Display(Name = "cursor_left")]
    CursorLeft,
    
    /// <summary>
    /// Directional pad right.
    /// </summary>
    [Display(Name = "cursor_right")]
    CursorRight,
    
    /// <summary>
    /// Directional pad enter.
    /// </summary>
    [Display(Name = "cursor_enter")]
    CursorEnter,
    
    /// <summary>
    /// Number pad digit 0.
    /// </summary>
    [Display(Name = "digit_0")]
    Digit0,
    
    /// <summary>
    /// Number pad digit 1.
    /// </summary>
    [Display(Name = "digit_1")]
    Digit1,
    
    /// <summary>
    /// Number pad digit 2.
    /// </summary>
    [Display(Name = "digit_2")]
    Digit2,
    
    /// <summary>
    /// Number pad digit 3.
    /// </summary>
    [Display(Name = "digit_3")]
    Digit3,
    
    /// <summary>
    /// Number pad digit 4.
    /// </summary>
    [Display(Name = "digit_4")]
    Digit4,
    
    /// <summary>
    /// Number pad digit 5.
    /// </summary>
    [Display(Name = "digit_5")]
    Digit5,
    
    /// <summary>
    /// Number pad digit 6.
    /// </summary>
    [Display(Name = "digit_6")]
    Digit6,
    
    /// <summary>
    /// Number pad digit 7.
    /// </summary>
    [Display(Name = "digit_7")]
    Digit7,
    
    /// <summary>
    /// Number pad digit 8.
    /// </summary>
    [Display(Name = "digit_8")]
    Digit8,
    
    /// <summary>
    /// Number pad digit 9.
    /// </summary>
    [Display(Name = "digit_9")]
    Digit9,
    
    /// <summary>
    /// Function red.
    /// </summary>
    [Display(Name = "function_red")]
    FunctionRed,
    
    /// <summary>
    /// Function green.
    /// </summary>
    [Display(Name = "function_green")]
    FunctionGreen,
    
    /// <summary>
    /// Function yellow.
    /// </summary>
    [Display(Name = "function_yellow")]
    FunctionYellow,
    
    /// <summary>
    /// Function blue.
    /// </summary>
    [Display(Name = "function_blue")]
    FunctionBlue,
    
    /// <summary>
    /// Home menu
    /// </summary>
    [Display(Name = "home")]
    Home,
    
    /// <summary>
    /// Menu
    /// </summary>
    [Display(Name = "menu")]
    Menu,
    
    /// <summary>
    /// Context menu
    /// </summary>
    [Display(Name = "context_menu")]
    ContextMenu,
    
    /// <summary>
    /// Program guide menu.
    /// </summary>
    [Display(Name = "guide")]
    Guide,
    
    /// <summary>
    /// Information menu / what's playing.
    /// </summary>
    [Display(Name = "info")]
    Info,
    
    /// <summary>
    /// Back / exit function for menu navigation (to exit menu, guide, info).
    /// </summary>
    [Display(Name = "back")]
    Back,
    
    /// <summary>
    /// Select an input source from the available sources.
    /// </summary>
    /// <remarks>Parameters: source</remarks>
    [Display(Name = "select_source")]
    SelectSource,
    
    /// <summary>
    /// Select a sound mode from the available modes.
    /// </summary>
    /// <remarks>Parameters: mode</remarks>
    [Display(Name = "select_sound_mode")]
    SelectSoundMode,
    
    /// <summary>
    /// Start, stop or open recording menu (device dependant).
    /// </summary>
    [Display(Name = "record")]
    Record,
    
    /// <summary>
    /// Open recordings.
    /// </summary>
    [Display(Name = "my_recordings")]
    MyRecordings,
    
    /// <summary>
    /// Switch to live view.
    /// </summary>
    [Display(Name = "live")]
    Live,
    
    /// <summary>
    /// Eject media.
    /// </summary>
    [Display(Name = "eject")]
    Eject,
    
    /// <summary>
    /// Open or close.
    /// </summary>
    [Display(Name = "open_close")]
    OpenClose,
    
    /// <summary>
    /// Switch or select audio track.
    /// </summary>
    [Display(Name = "audio_track")]
    AudioTrack,
    
    /// <summary>
    /// Switch or select subtitle.
    /// </summary>
    [Display(Name = "subtitle")]
    Subtitle,
    
    /// <summary>
    /// Settings menu
    /// </summary>
    [Display(Name = "settings")]
    Settings,
    
    [Display(Name = "search")]
    Search,
    
    Dimmer,
    PureAudioToggle,
    Clear,
    
    [Display(Name = EntitySettingsConstants.PopUpMenu)]
    PopUpMenu,
    
    Pause,
    Play,
    Angle,
    Zoom,
    
    [Display(Name = EntitySettingsConstants.SecondaryAudioProgram)]
    SecondaryAudioProgram,
    
    [Display(Name = EntitySettingsConstants.AbReplay)]
    AbReplay,
    
    [Display(Name = EntitySettingsConstants.PictureInPicture)]
    PictureInPicture,
    
    Resolution,
    
    [Display(Name = EntitySettingsConstants.SubtitleHold)]
    SubtitleHold,
    
    Option,
    
    [Display(Name = EntitySettingsConstants.ThreeD)]
    ThreeD,
    
    [Display(Name = EntitySettingsConstants.PictureAdjustment)]
    PictureAdjustment,
    
    Hdr,
    
    [Display(Name = EntitySettingsConstants.InfoHold)]
    InfoHold,
    
    [Display(Name = EntitySettingsConstants.ResolutionHold)]
    ResolutionHold,
    
    [Display(Name = EntitySettingsConstants.AvSync)]
    AvSync,
    
    [Display(Name = EntitySettingsConstants.GaplessPlay)]
    GaplessPlay
}

[EnumJsonConverter(typeof(OppoCommandId), CaseSensitive = false, PropertyName = "cmd_id")]
public partial class OppoCommandIdJsonConverter;