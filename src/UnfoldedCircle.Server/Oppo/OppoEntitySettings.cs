using System.Collections.Frozen;

using Oppo;

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

    public static readonly FrozenDictionary<OppoModel, string[]> SourceList = new Dictionary<OppoModel, string[]>
    {
        [OppoModel.BDP10X] =
        [
            OppoConstants.InputSource.BluRayPlayer,
            OppoConstants.InputSource.HDMIFront,
            OppoConstants.InputSource.HDMIBack,
            OppoConstants.InputSource.ARCHDMIOut1,
            OppoConstants.InputSource.ARCHDMIOut2,
            OppoConstants.InputSource.Optical,
            OppoConstants.InputSource.Coaxial,
            OppoConstants.InputSource.USBAudio
        ],
        [OppoModel.UDP203] =
        [
            OppoConstants.InputSource.BluRayPlayer,
            OppoConstants.InputSource.HDMIIn,
            OppoConstants.InputSource.ARCHDMIOut
        ],
        [OppoModel.UDP205] =
        [
            OppoConstants.InputSource.BluRayPlayer,
            OppoConstants.InputSource.HDMIIn,
            OppoConstants.InputSource.ARCHDMIOut,
            OppoConstants.InputSource.Optical,
            OppoConstants.InputSource.Coaxial,
            OppoConstants.InputSource.USBAudio
        ]
    }.ToFrozenDictionary();
    
    public static readonly FrozenDictionary<string, InputSource> SourceMap = new Dictionary<string, InputSource>(StringComparer.OrdinalIgnoreCase)
    {
        [OppoConstants.InputSource.BluRayPlayer] = InputSource.BluRayPlayer,
        [OppoConstants.InputSource.HDMIFront] = InputSource.HDMIIn,
        [OppoConstants.InputSource.HDMIBack] = InputSource.HDMIIn,
        [OppoConstants.InputSource.ARCHDMIOut1] = InputSource.ARCHDMIOut,
        [OppoConstants.InputSource.ARCHDMIOut2] = InputSource.ARCHDMIOut,
        [OppoConstants.InputSource.Optical] = InputSource.Optical,
        [OppoConstants.InputSource.Coaxial] = InputSource.Coaxial,
        [OppoConstants.InputSource.USBAudio] = InputSource.USBAudio,
        [OppoConstants.InputSource.HDMIIn] = InputSource.HDMIIn,
        [OppoConstants.InputSource.ARCHDMIOut] = InputSource.ARCHDMIOut
    }.ToFrozenDictionary();
}