using System.ComponentModel.DataAnnotations;
using Theodicean.SourceGenerators;

namespace UnfoldedCircle.Models.Sync;

[JsonConverter(typeof(OppoCommandIdJsonConverter))]
public enum OppoCommandId
{
    /// <summary>
    /// Switch on media player.
    /// </summary>
    [Display(Name = MediaPlayerCommandIdConstants.On)]
    On = 1,

    /// <summary>
    /// Switch off media player.
    /// </summary>
    [Display(Name = MediaPlayerCommandIdConstants.Off)]
    Off,

    /// <summary>
    /// Toggle the current power state, either from on -> off or from off -> on.
    /// </summary>
    [Display(Name = MediaPlayerCommandIdConstants.Toggle)]
    Toggle,

    /// <summary>
    /// Toggle play / pause.
    /// </summary>
    [Display(Name = MediaPlayerCommandIdConstants.PlayPause)]
    PlayPause,

    /// <summary>
    /// Stop playback.
    /// </summary>
    [Display(Name = MediaPlayerCommandIdConstants.Stop)]
    Stop,

    /// <summary>
    /// Go back to previous track.
    /// </summary>
    [Display(Name = MediaPlayerCommandIdConstants.Previous)]
    Previous,

    /// <summary>
    /// Skip to next track.
    /// </summary>
    [Display(Name = MediaPlayerCommandIdConstants.Next)]
    Next,

    /// <summary>
    /// Fast forward current track.
    /// </summary>
    [Display(Name = MediaPlayerCommandIdConstants.FastForward)]
    FastForward,

    /// <summary>
    /// Rewind current track.
    /// </summary>
    [Display(Name = MediaPlayerCommandIdConstants.Rewind)]
    Rewind,

    /// <summary>
    /// Seek to given position in current track. Position is given in seconds.
    /// </summary>
    /// <remarks>Parameters: media_position</remarks>
    [Display(Name = MediaPlayerCommandIdConstants.Seek)]
    Seek,

    /// <summary>
    /// Set volume to given level.
    /// </summary>
    /// <remarks>Parameters: volume</remarks>
    [Display(Name = MediaPlayerCommandIdConstants.Volume)]
    Volume,

    /// <summary>
    /// Increase volume.
    /// </summary>
    [Display(Name = MediaPlayerCommandIdConstants.VolumeUp)]
    VolumeUp,

    /// <summary>
    /// Decrease volume.
    /// </summary>
    [Display(Name = MediaPlayerCommandIdConstants.VolumeDown)]
    VolumeDown,

    /// <summary>
    /// Toggle mute state.
    /// </summary>
    [Display(Name = MediaPlayerCommandIdConstants.MuteToggle)]
    MuteToggle,

    /// <summary>
    /// Mute volume.
    /// </summary>
    [Display(Name = MediaPlayerCommandIdConstants.Mute)]
    Mute,

    /// <summary>
    /// Unmute volume.
    /// </summary>
    [Display(Name = MediaPlayerCommandIdConstants.Unmute)]
    Unmute,

    /// <summary>
    /// Repeat track or playlist.
    /// </summary>
    /// <remarks>Parameters: repeat</remarks>
    [Display(Name = MediaPlayerCommandIdConstants.Repeat)]
    Repeat,

    /// <summary>
    /// Shuffle playlist or start random playback.
    /// </summary>
    /// <remarks>Parameters: shuffle</remarks>
    [Display(Name = MediaPlayerCommandIdConstants.Shuffle)]
    Shuffle,

    /// <summary>
    /// Channel up.
    /// </summary>
    [Display(Name = MediaPlayerCommandIdConstants.ChannelUp)]
    ChannelUp,

    /// <summary>
    /// Channel down.
    /// </summary>
    [Display(Name = MediaPlayerCommandIdConstants.ChannelDown)]
    ChannelDown,

    /// <summary>
    /// Directional pad up.
    /// </summary>
    [Display(Name = MediaPlayerCommandIdConstants.CursorUp)]
    CursorUp,

    /// <summary>
    /// Directional pad down.
    /// </summary>
    [Display(Name = MediaPlayerCommandIdConstants.CursorDown)]
    CursorDown,

    /// <summary>
    /// Directional pad left.
    /// </summary>
    [Display(Name = MediaPlayerCommandIdConstants.CursorLeft)]
    CursorLeft,

    /// <summary>
    /// Directional pad right.
    /// </summary>
    [Display(Name = MediaPlayerCommandIdConstants.CursorRight)]
    CursorRight,

    /// <summary>
    /// Directional pad enter.
    /// </summary>
    [Display(Name = MediaPlayerCommandIdConstants.CursorEnter)]
    CursorEnter,

    /// <summary>
    /// Number pad digit 0.
    /// </summary>
    [Display(Name = MediaPlayerCommandIdConstants.Digit0)]
    Digit0,

    /// <summary>
    /// Number pad digit 1.
    /// </summary>
    [Display(Name = MediaPlayerCommandIdConstants.Digit1)]
    Digit1,

    /// <summary>
    /// Number pad digit 2.
    /// </summary>
    [Display(Name = MediaPlayerCommandIdConstants.Digit2)]
    Digit2,

    /// <summary>
    /// Number pad digit 3.
    /// </summary>
    [Display(Name = MediaPlayerCommandIdConstants.Digit3)]
    Digit3,

    /// <summary>
    /// Number pad digit 4.
    /// </summary>
    [Display(Name = MediaPlayerCommandIdConstants.Digit4)]
    Digit4,

    /// <summary>
    /// Number pad digit 5.
    /// </summary>
    [Display(Name = MediaPlayerCommandIdConstants.Digit5)]
    Digit5,

    /// <summary>
    /// Number pad digit 6.
    /// </summary>
    [Display(Name = MediaPlayerCommandIdConstants.Digit6)]
    Digit6,

    /// <summary>
    /// Number pad digit 7.
    /// </summary>
    [Display(Name = MediaPlayerCommandIdConstants.Digit7)]
    Digit7,

    /// <summary>
    /// Number pad digit 8.
    /// </summary>
    [Display(Name = MediaPlayerCommandIdConstants.Digit8)]
    Digit8,

    /// <summary>
    /// Number pad digit 9.
    /// </summary>
    [Display(Name = MediaPlayerCommandIdConstants.Digit9)]
    Digit9,

    /// <summary>
    /// Function red.
    /// </summary>
    [Display(Name = MediaPlayerCommandIdConstants.FunctionRed)]
    FunctionRed,

    /// <summary>
    /// Function green.
    /// </summary>
    [Display(Name = MediaPlayerCommandIdConstants.FunctionGreen)]
    FunctionGreen,

    /// <summary>
    /// Function yellow.
    /// </summary>
    [Display(Name = MediaPlayerCommandIdConstants.FunctionYellow)]
    FunctionYellow,

    /// <summary>
    /// Function blue.
    /// </summary>
    [Display(Name = MediaPlayerCommandIdConstants.FunctionBlue)]
    FunctionBlue,

    /// <summary>
    /// Home menu
    /// </summary>
    [Display(Name = MediaPlayerCommandIdConstants.Home)]
    Home,

    /// <summary>
    /// Menu
    /// </summary>
    [Display(Name = MediaPlayerCommandIdConstants.Menu)]
    Menu,

    /// <summary>
    /// Context menu
    /// </summary>
    [Display(Name = MediaPlayerCommandIdConstants.ContextMenu)]
    ContextMenu,

    /// <summary>
    /// Program guide menu.
    /// </summary>
    [Display(Name = MediaPlayerCommandIdConstants.Guide)]
    Guide,

    /// <summary>
    /// Information menu / what's playing.
    /// </summary>
    [Display(Name = MediaPlayerCommandIdConstants.Info)]
    Info,

    /// <summary>
    /// Back / exit function for menu navigation (to exit menu, guide, info).
    /// </summary>
    [Display(Name = MediaPlayerCommandIdConstants.Back)]
    Back,

    /// <summary>
    /// Select an input source from the available sources.
    /// </summary>
    /// <remarks>Parameters: source</remarks>
    [Display(Name = MediaPlayerCommandIdConstants.SelectSource)]
    SelectSource,

    /// <summary>
    /// Select a sound mode from the available modes.
    /// </summary>
    /// <remarks>Parameters: mode</remarks>
    [Display(Name = MediaPlayerCommandIdConstants.SelectSoundMode)]
    SelectSoundMode,

    /// <summary>
    /// Start, stop or open recording menu (device dependant).
    /// </summary>
    [Display(Name = MediaPlayerCommandIdConstants.Record)]
    Record,

    /// <summary>
    /// Open recordings.
    /// </summary>
    [Display(Name = MediaPlayerCommandIdConstants.MyRecordings)]
    MyRecordings,

    /// <summary>
    /// Switch to live view.
    /// </summary>
    [Display(Name = MediaPlayerCommandIdConstants.Live)]
    Live,

    /// <summary>
    /// Eject media.
    /// </summary>
    [Display(Name = MediaPlayerCommandIdConstants.Eject)]
    Eject,

    /// <summary>
    /// Open or close.
    /// </summary>
    [Display(Name = MediaPlayerCommandIdConstants.OpenClose)]
    OpenClose,

    /// <summary>
    /// Switch or select audio track.
    /// </summary>
    [Display(Name = MediaPlayerCommandIdConstants.AudioTrack)]
    AudioTrack,

    /// <summary>
    /// Switch or select subtitle.
    /// </summary>
    [Display(Name = MediaPlayerCommandIdConstants.Subtitle)]
    Subtitle,

    /// <summary>
    /// Settings menu
    /// </summary>
    [Display(Name = MediaPlayerCommandIdConstants.Settings)]
    Settings,

    [Display(Name = MediaPlayerCommandIdConstants.Search)]
    Search,
    
    Dimmer,

    [Display(Name = EntitySettingsConstants.PureAudioToggle)]
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