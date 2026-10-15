using UnfoldedCircle.Models;
using UnfoldedCircle.Models.Sync;

namespace UnfoldedCircle.OppoBluRay.OppoEntity;

[JsonConverter(typeof(StrictEnumConverter<OppoCommandId>))]
public enum OppoCommandId : byte
{
    /// <summary>
    /// Switch on media player.
    /// </summary>
    [JsonStringEnumMemberName(MediaPlayerCommandIdConstants.On)]
    On = 1,

    /// <summary>
    /// Switch off media player.
    /// </summary>
    [JsonStringEnumMemberName(MediaPlayerCommandIdConstants.Off)]
    Off,

    /// <summary>
    /// Toggle the current power state, either from on -> off or from off -> on.
    /// </summary>
    [JsonStringEnumMemberName(MediaPlayerCommandIdConstants.Toggle)]
    Toggle,

    /// <summary>
    /// Toggle play / pause.
    /// </summary>
    [JsonStringEnumMemberName(MediaPlayerCommandIdConstants.PlayPause)]
    PlayPause,

    /// <summary>
    /// Stop playback.
    /// </summary>
    [JsonStringEnumMemberName(MediaPlayerCommandIdConstants.Stop)]
    Stop,

    /// <summary>
    /// Go back to previous track.
    /// </summary>
    [JsonStringEnumMemberName(MediaPlayerCommandIdConstants.Previous)]
    Previous,

    /// <summary>
    /// Skip to next track.
    /// </summary>
    [JsonStringEnumMemberName(MediaPlayerCommandIdConstants.Next)]
    Next,

    /// <summary>
    /// Fast forward current track.
    /// </summary>
    [JsonStringEnumMemberName(MediaPlayerCommandIdConstants.FastForward)]
    FastForward,

    /// <summary>
    /// Rewind current track.
    /// </summary>
    [JsonStringEnumMemberName(MediaPlayerCommandIdConstants.Rewind)]
    Rewind,

    /// <summary>
    /// Seek to given position in current track. Position is given in seconds.
    /// </summary>
    /// <remarks>Parameters: media_position</remarks>
    [JsonStringEnumMemberName(MediaPlayerCommandIdConstants.Seek)]
    Seek,

    /// <summary>
    /// Set volume to given level.
    /// </summary>
    /// <remarks>Parameters: volume</remarks>
    [JsonStringEnumMemberName(MediaPlayerCommandIdConstants.Volume)]
    Volume,

    /// <summary>
    /// Increase volume.
    /// </summary>
    [JsonStringEnumMemberName(MediaPlayerCommandIdConstants.VolumeUp)]
    VolumeUp,

    /// <summary>
    /// Decrease volume.
    /// </summary>
    [JsonStringEnumMemberName(MediaPlayerCommandIdConstants.VolumeDown)]
    VolumeDown,

    /// <summary>
    /// Toggle mute state.
    /// </summary>
    [JsonStringEnumMemberName(MediaPlayerCommandIdConstants.MuteToggle)]
    MuteToggle,

    /// <summary>
    /// Mute volume.
    /// </summary>
    [JsonStringEnumMemberName(MediaPlayerCommandIdConstants.Mute)]
    Mute,

    /// <summary>
    /// Unmute volume.
    /// </summary>
    [JsonStringEnumMemberName(MediaPlayerCommandIdConstants.Unmute)]
    Unmute,

    /// <summary>
    /// Repeat track or playlist.
    /// </summary>
    /// <remarks>Parameters: repeat</remarks>
    [JsonStringEnumMemberName(MediaPlayerCommandIdConstants.Repeat)]
    Repeat,

    /// <summary>
    /// Shuffle playlist or start random playback.
    /// </summary>
    /// <remarks>Parameters: shuffle</remarks>
    [JsonStringEnumMemberName(MediaPlayerCommandIdConstants.Shuffle)]
    Shuffle,

    /// <summary>
    /// Channel up.
    /// </summary>
    [JsonStringEnumMemberName(MediaPlayerCommandIdConstants.ChannelUp)]
    ChannelUp,

    /// <summary>
    /// Channel down.
    /// </summary>
    [JsonStringEnumMemberName(MediaPlayerCommandIdConstants.ChannelDown)]
    ChannelDown,

    /// <summary>
    /// Directional pad up.
    /// </summary>
    [JsonStringEnumMemberName(MediaPlayerCommandIdConstants.CursorUp)]
    CursorUp,

    /// <summary>
    /// Directional pad down.
    /// </summary>
    [JsonStringEnumMemberName(MediaPlayerCommandIdConstants.CursorDown)]
    CursorDown,

    /// <summary>
    /// Directional pad left.
    /// </summary>
    [JsonStringEnumMemberName(MediaPlayerCommandIdConstants.CursorLeft)]
    CursorLeft,

    /// <summary>
    /// Directional pad right.
    /// </summary>
    [JsonStringEnumMemberName(MediaPlayerCommandIdConstants.CursorRight)]
    CursorRight,

    /// <summary>
    /// Directional pad enter.
    /// </summary>
    [JsonStringEnumMemberName(MediaPlayerCommandIdConstants.CursorEnter)]
    CursorEnter,

    /// <summary>
    /// Number pad digit 0.
    /// </summary>
    [JsonStringEnumMemberName(MediaPlayerCommandIdConstants.Digit0)]
    Digit0,

    /// <summary>
    /// Number pad digit 1.
    /// </summary>
    [JsonStringEnumMemberName(MediaPlayerCommandIdConstants.Digit1)]
    Digit1,

    /// <summary>
    /// Number pad digit 2.
    /// </summary>
    [JsonStringEnumMemberName(MediaPlayerCommandIdConstants.Digit2)]
    Digit2,

    /// <summary>
    /// Number pad digit 3.
    /// </summary>
    [JsonStringEnumMemberName(MediaPlayerCommandIdConstants.Digit3)]
    Digit3,

    /// <summary>
    /// Number pad digit 4.
    /// </summary>
    [JsonStringEnumMemberName(MediaPlayerCommandIdConstants.Digit4)]
    Digit4,

    /// <summary>
    /// Number pad digit 5.
    /// </summary>
    [JsonStringEnumMemberName(MediaPlayerCommandIdConstants.Digit5)]
    Digit5,

    /// <summary>
    /// Number pad digit 6.
    /// </summary>
    [JsonStringEnumMemberName(MediaPlayerCommandIdConstants.Digit6)]
    Digit6,

    /// <summary>
    /// Number pad digit 7.
    /// </summary>
    [JsonStringEnumMemberName(MediaPlayerCommandIdConstants.Digit7)]
    Digit7,

    /// <summary>
    /// Number pad digit 8.
    /// </summary>
    [JsonStringEnumMemberName(MediaPlayerCommandIdConstants.Digit8)]
    Digit8,

    /// <summary>
    /// Number pad digit 9.
    /// </summary>
    [JsonStringEnumMemberName(MediaPlayerCommandIdConstants.Digit9)]
    Digit9,

    /// <summary>
    /// Function red.
    /// </summary>
    [JsonStringEnumMemberName(MediaPlayerCommandIdConstants.FunctionRed)]
    FunctionRed,

    /// <summary>
    /// Function green.
    /// </summary>
    [JsonStringEnumMemberName(MediaPlayerCommandIdConstants.FunctionGreen)]
    FunctionGreen,

    /// <summary>
    /// Function yellow.
    /// </summary>
    [JsonStringEnumMemberName(MediaPlayerCommandIdConstants.FunctionYellow)]
    FunctionYellow,

    /// <summary>
    /// Function blue.
    /// </summary>
    [JsonStringEnumMemberName(MediaPlayerCommandIdConstants.FunctionBlue)]
    FunctionBlue,

    /// <summary>
    /// Home menu
    /// </summary>
    [JsonStringEnumMemberName(MediaPlayerCommandIdConstants.Home)]
    Home,

    /// <summary>
    /// Menu
    /// </summary>
    [JsonStringEnumMemberName(MediaPlayerCommandIdConstants.Menu)]
    Menu,

    /// <summary>
    /// Context menu
    /// </summary>
    [JsonStringEnumMemberName(MediaPlayerCommandIdConstants.ContextMenu)]
    ContextMenu,

    /// <summary>
    /// Program guide menu.
    /// </summary>
    [JsonStringEnumMemberName(MediaPlayerCommandIdConstants.Guide)]
    Guide,

    /// <summary>
    /// Information menu / what's playing.
    /// </summary>
    [JsonStringEnumMemberName(MediaPlayerCommandIdConstants.Info)]
    Info,

    /// <summary>
    /// Back / exit function for menu navigation (to exit menu, guide, info).
    /// </summary>
    [JsonStringEnumMemberName(MediaPlayerCommandIdConstants.Back)]
    Back,

    /// <summary>
    /// Select an input source from the available sources.
    /// </summary>
    /// <remarks>Parameters: source</remarks>
    [JsonStringEnumMemberName(MediaPlayerCommandIdConstants.SelectSource)]
    SelectSource,

    /// <summary>
    /// Select a sound mode from the available modes.
    /// </summary>
    /// <remarks>Parameters: mode</remarks>
    [JsonStringEnumMemberName(MediaPlayerCommandIdConstants.SelectSoundMode)]
    SelectSoundMode,

    /// <summary>
    /// Start, stop or open recording menu (device dependant).
    /// </summary>
    [JsonStringEnumMemberName(MediaPlayerCommandIdConstants.Record)]
    Record,

    /// <summary>
    /// Open recordings.
    /// </summary>
    [JsonStringEnumMemberName(MediaPlayerCommandIdConstants.MyRecordings)]
    MyRecordings,

    /// <summary>
    /// Switch to live view.
    /// </summary>
    [JsonStringEnumMemberName(MediaPlayerCommandIdConstants.Live)]
    Live,

    /// <summary>
    /// Eject media.
    /// </summary>
    [JsonStringEnumMemberName(MediaPlayerCommandIdConstants.Eject)]
    Eject,

    /// <summary>
    /// Open or close.
    /// </summary>
    [JsonStringEnumMemberName(MediaPlayerCommandIdConstants.OpenClose)]
    OpenClose,

    /// <summary>
    /// Switch or select audio track.
    /// </summary>
    [JsonStringEnumMemberName(MediaPlayerCommandIdConstants.AudioTrack)]
    AudioTrack,

    /// <summary>
    /// Switch or select subtitle.
    /// </summary>
    [JsonStringEnumMemberName(MediaPlayerCommandIdConstants.Subtitle)]
    Subtitle,

    /// <summary>
    /// Settings menu
    /// </summary>
    [JsonStringEnumMemberName(MediaPlayerCommandIdConstants.Settings)]
    Settings,

    [JsonStringEnumMemberName(MediaPlayerCommandIdConstants.Search)]
    Search,

    Dimmer,

    [JsonStringEnumMemberName(EntitySettingsConstants.PureAudioToggle)]
    PureAudioToggle,

    [JsonStringEnumMemberName(EntitySettingsConstants.Clear)]
    Clear,

    [JsonStringEnumMemberName(EntitySettingsConstants.PopUpMenu)]
    PopUpMenu,

    Pause,
    Play,
    Angle,
    Zoom,

    [JsonStringEnumMemberName(EntitySettingsConstants.SecondaryAudioProgram)]
    SecondaryAudioProgram,

    [JsonStringEnumMemberName(EntitySettingsConstants.AbReplay)]
    AbReplay,

    [JsonStringEnumMemberName(EntitySettingsConstants.PictureInPicture)]
    PictureInPicture,

    Resolution,

    [JsonStringEnumMemberName(EntitySettingsConstants.SubtitleHold)]
    SubtitleHold,

    Option,

    [JsonStringEnumMemberName(EntitySettingsConstants.ThreeD)]
    ThreeD,

    [JsonStringEnumMemberName(EntitySettingsConstants.PictureAdjustment)]
    PictureAdjustment,

    [JsonStringEnumMemberName(EntitySettingsConstants.Hdr)]
    Hdr,

    [JsonStringEnumMemberName(EntitySettingsConstants.InfoHold)]
    InfoHold,

    [JsonStringEnumMemberName(EntitySettingsConstants.ResolutionHold)]
    ResolutionHold,

    [JsonStringEnumMemberName(EntitySettingsConstants.AvSync)]
    AvSync,

    [JsonStringEnumMemberName(EntitySettingsConstants.GaplessPlay)]
    GaplessPlay
}
