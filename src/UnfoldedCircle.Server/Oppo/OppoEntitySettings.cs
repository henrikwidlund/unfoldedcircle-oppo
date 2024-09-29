using System.Collections.Frozen;
using UnfoldedCircle.Models.Sync;

namespace UnfoldedCircle.Server.Oppo;

public static class OppoEntitySettings
{
    public static readonly FrozenSet<MediaPlayerEntityFeature> MediaPlayerEntityFeatures = new[]
    {
        MediaPlayerEntityFeature.OnOff,
        MediaPlayerEntityFeature.Toggle,
        MediaPlayerEntityFeature.Volume,
        MediaPlayerEntityFeature.VolumeUpDown,
        MediaPlayerEntityFeature.MuteToggle,
        MediaPlayerEntityFeature.PlayPause,
        MediaPlayerEntityFeature.Stop,
        MediaPlayerEntityFeature.Next,
        MediaPlayerEntityFeature.Previous,
        MediaPlayerEntityFeature.FastForward,
        MediaPlayerEntityFeature.Rewind,
        MediaPlayerEntityFeature.Repeat,
        MediaPlayerEntityFeature.Shuffle,
        MediaPlayerEntityFeature.Seek,
        MediaPlayerEntityFeature.MediaDuration,
        MediaPlayerEntityFeature.MediaPosition,
        MediaPlayerEntityFeature.MediaTitle,
        MediaPlayerEntityFeature.MediaArtist,
        MediaPlayerEntityFeature.MediaAlbum,
        MediaPlayerEntityFeature.MediaImageUrl,
        MediaPlayerEntityFeature.MediaType,
        MediaPlayerEntityFeature.Dpad,
        MediaPlayerEntityFeature.Numpad,
        MediaPlayerEntityFeature.Home,
        MediaPlayerEntityFeature.Menu,
        MediaPlayerEntityFeature.ContextMenu,
        MediaPlayerEntityFeature.Info,
        MediaPlayerEntityFeature.ColorButtons,
        MediaPlayerEntityFeature.ChannelSwitcher,
        MediaPlayerEntityFeature.SelectSource,
        MediaPlayerEntityFeature.OpenClose,
        MediaPlayerEntityFeature.AudioTrack,
        MediaPlayerEntityFeature.Subtitle,
        MediaPlayerEntityFeature.Settings
    }.ToFrozenSet();
    
    public static readonly FrozenSet<string> SimpleCommands = new[]
    {
        EntitySettingsConstants.Dimmer,
        EntitySettingsConstants.PureAudioToggle,
        EntitySettingsConstants.Clear,
        EntitySettingsConstants.InfoToggle,
        EntitySettingsConstants.TopMenu,
        EntitySettingsConstants.PopUpMenu,
        EntitySettingsConstants.Pause,
        EntitySettingsConstants.Play,
        EntitySettingsConstants.Angle,
        EntitySettingsConstants.Zoom,
        EntitySettingsConstants.SecondaryAudioProgram,
        EntitySettingsConstants.AbReplay,
        EntitySettingsConstants.PictureInPicture,
        EntitySettingsConstants.Resolution,
        EntitySettingsConstants.SubtitleHold,
        EntitySettingsConstants.Option,
        EntitySettingsConstants.ThreeD,
        EntitySettingsConstants.PictureAdjustment,
        EntitySettingsConstants.Hdr,
        EntitySettingsConstants.InfoHold,
        EntitySettingsConstants.ResolutionHold,
        EntitySettingsConstants.AvSync,
        EntitySettingsConstants.GaplessPlay
    }.ToFrozenSet();
}