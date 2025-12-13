using System.ComponentModel.DataAnnotations;

using NetEscapades.EnumGenerators;

namespace Oppo;

public enum PowerState : sbyte
{
    On = 1,
    Off,
    Unknown
}

public enum TrayState : sbyte
{
    Open = 1,
    Closed,
    Unknown
}

public enum DimmerState : sbyte
{
    On = 1,
    Dim,
    Off,
    Unknown
}

public enum PureAudioState : sbyte
{
    On = 1,
    Off,
    Unknown
}

public enum MuteState : sbyte
{
    On = 1,
    Off,
    Unknown
}

// ReSharper disable InconsistentNaming
public enum ABReplayState : sbyte
{
    A = 1,
    AB,
    Off,
    Unknown
}
// ReSharper restore InconsistentNaming

public enum RepeatState : sbyte
{
    RepeatChapter = 1,
    RepeatTitle,
    Off,
    Unknown
}

public enum RepeatMode : sbyte
{
    /// <summary>
    /// Only used if return value is unknown. Do not use this value to set the repeat mode.
    /// </summary>
    Unknown,
    
    /// <summary>
    /// Repeat chapter
    /// </summary>
    Chapter,
    
    /// <summary>
    /// Repeat title or CD track
    /// </summary>
    Title,
    
    /// <summary>
    /// Repeat all
    /// </summary>
    All,
    
    /// <summary>
    /// Repeat off
    /// </summary>
    Off,
    
    /// <summary>
    /// Shuffle
    /// </summary>
    Shuffle,
    
    /// <summary>
    /// Random
    /// </summary>
    Random
}

public enum PlaybackStatus : sbyte
{
    Unknown,
    Play,
    Pause,
    Stop,
    Step,
    FastRewind,
    FastForward,
    SlowForward,
    SlowRewind,
    Setup,
    HomeMenu,
    MediaCenter,
    ScreenSaver,
    DiscMenu,
    
    // Pre 20X models
    NoDisc,
    Loading,
    Open,
    Close
}

[EnumExtensions(MetadataSource = MetadataSource.DisplayAttribute)]
public enum DiscType : sbyte
{
    // ReSharper disable InconsistentNaming
    [Display(Name = "Blu-Ray Movie")]
    BlueRayMovie = 1,

    [Display(Name = "DVD Video")]
    DVDVideo,

    [Display(Name = "DVD Audio")]
    DVDAudio,

    SACD,

    [Display(Name = "CD Audio")]
    CDDiscAudio,

    [Display(Name = "Data Disc")]
    DataDisc,

    [Display(Name = "Ultra HD Blu-Ray")]
    UltraHDBluRay,

    [Display(Name = "No Disc")]
    NoDisc,

    [Display(Name = "Unknown Disc")]
    UnknownDisc,
    
    // Pre 20X models
    HDCD
    // ReSharper restore InconsistentNaming
}

public enum CurrentRepeatMode : sbyte
{
    Unknown,
    Off,
    RepeatOne,
    RepeatChapter,
    RepeatAll,
    RepeatTitle,
    Shuffle,
    Random
}

public enum OppoModel : sbyte
{
    // ReSharper disable InconsistentNaming
    BDP83,
    BDP9X,
    BDP10X,
    UDP203,
    UDP205
    // ReSharper restore InconsistentNaming
}

public static class OppoModelExtensions
{
    extension(OppoModel model)
    {
        public string ToStringFast() =>
            model switch
            {
                OppoModel.BDP83 => nameof(OppoModel.BDP83),
                OppoModel.BDP9X => nameof(OppoModel.BDP9X),
                OppoModel.BDP10X => nameof(OppoModel.BDP10X),
                OppoModel.UDP203 => nameof(OppoModel.UDP203),
                OppoModel.UDP205 => nameof(OppoModel.UDP205),
                _ => "Unknown"
            };
    }
}

[EnumExtensions(MetadataSource = MetadataSource.DisplayAttribute)]
public enum InputSource : sbyte
{
    // ReSharper disable InconsistentNaming
    Unknown,

    [Display(Name = "Blu-Ray Player")]
    BluRayPlayer,
    
    // 20x models,
    [Display(Name = "HDMI In")]
    HDMIIn,
    [Display(Name = "ARC HDMI Out")]
    ARCHDMIOut,
    
    // 10x and 205 models
    [Display(Name = "Optical")]
    Optical,

    [Display(Name = "Coaxial")]
    Coaxial,

    [Display(Name = "USB Audio")]
    USBAudio,
    
    // pre 20x models
    [Display(Name = "HDMI Frot")]
    HDMIFront,

    [Display(Name = "HDMI Back")]
    HDMIBack,

    [Display(Name = "ARC HDMI Out 1")]
    ARCHDMIOut1,

    [Display(Name = "ARC HDMI Out 2")]
    ARCHDMIOut2
    // ReSharper restore InconsistentNaming
}

public enum VerboseMode : sbyte
{
    Unknown,

    /// <summary>
    /// Set Verbose Mode to off
    /// </summary>
    Off,

    /// <summary>
    /// Commands are echoed back in the response
    /// </summary>
    EchoCommandsInResponse,

    /// <summary>
    /// Enable unsolicited status updates. Only major status changes are reported.
    /// </summary>
    ModeUnsolicitedStatusUpdates,

    /// <summary>
    /// Enable detailed status updates. When content is playing, the player sends out playback time updates every second.
    /// </summary>
    DetailedStatus
}

[EnumExtensions(MetadataSource = MetadataSource.DisplayAttribute)]
public enum HDMIResolution : sbyte
{
    Unknown,

    [Display(Name = "480i")]
    R480i,

    [Display(Name = "480p")]
    R480p,

    [Display(Name = "576i")]
    R576i,

    [Display(Name = "576p")]
    R576p,

    [Display(Name = "720p 50Hz")]
    R720p50,

    [Display(Name = "720p 60Hz")]
    R720p60,

    [Display(Name = "1080i 50Hz")]
    R1080i50,

    [Display(Name = "1080i 60Hz")]
    R1080i60,

    [Display(Name = "1080p 24Hz")]
    R1080p24,

    [Display(Name = "1080p 50Hz")]
    R1080p50,

    [Display(Name = "1080p 60Hz")]
    R1080p60,

    [Display(Name = "1080p Auto")]
    R1080PAuto,

    [Display(Name = "4K Ultra HD 24Hz")]
    RUltraHDp24,

    [Display(Name = "4K Ultra HD 50Hz")]
    RUltraHDp50,

    [Display(Name = "4K Ultra HD 60Hz")]
    RUltraHDp60,

    [Display(Name = "4K Ultra HD Auto")]
    RUltraHDAuto,

    Auto,

    [Display(Name = "Source Direct")]
    SourceDirect
}

[EnumExtensions(MetadataSource = MetadataSource.DisplayAttribute)]
public enum HDRStatus : sbyte
{
    Unknown,

    [Display(Name = "HDR10")]
    HDR,

    [Display(Name = "SDR")]
    SDR,

    [Display(Name = "Dolby Vision")]
    DolbyVision
}

[EnumExtensions(MetadataSource = MetadataSource.DisplayAttribute)]
public enum AspectRatio : sbyte
{
    Unknown,

    /// <summary>
    /// 16:9 Wide
    /// </summary>
    [Display(Name = "16:9 Wide")]
    A16WW,

    /// <summary>
    /// 16:9 Wide Auto, currently wide
    /// </summary>
    [Display(Name = "16:9 Wide Auto - Wide")]
    A16AW,

    /// <summary>
    /// 16:9 Wide Auto, currently playing 4:3
    /// </summary>
    [Display(Name = "16:9 Wide Auto - 4:3")]
    A169A,

    /// <summary>
    /// 21:9 Movable, currently full screen mode
    /// </summary>
    [Display(Name = "21:9 Movable - Full Screen Mode")]
    A21M0,

    /// <summary>
    /// 21:9 Movable, currently playing 16:9 or 4:3 content
    /// </summary>
    [Display(Name = "21:9 Movable - 16:9 or 4:3 Content")]
    A21M1,

    /// <summary>
    /// 21:9 Movable, currently playing 21:9 content
    /// </summary>
    [Display(Name = "21:9 Movable - 21:9 Content")]
    A21M2,

    /// <summary>
    /// 21:9 Fixed, currently full screen mode
    /// </summary>
    [Display(Name = "21:9 Fixed - Full Screen Mode")]
    A21F0,

    /// <summary>
    /// 21:9 Fixed, currently playing 16:9 or 4:3 content
    /// </summary>
    [Display(Name = "21:9 Fixed - 16:9 or 4:3 Content")]
    A21F1,

    /// <summary>
    /// 21:9 Fixed, currently playing 21:9 content
    /// </summary>
    [Display(Name = "21:9 Fixed - 21:9 Content")]
    A21F2,

    /// <summary>
    /// 21:9 Cropped, currently full screen mode
    /// </summary>
    [Display(Name = "21:9 Cropped - Full Screen Mode")]
    A21C0,

    /// <summary>
    /// 21:9 Cropped, currently playing 16:9 or 4:3 content
    /// </summary>
    [Display(Name = "21:9 Cropped - 16:9 or 4:3 Content")]
    A21C1,

    /// <summary>
    /// 21:9 Cropped, currently playing 21:9 content
    /// </summary>
    [Display(Name = "21:9 Cropped - 21:9 Content")]
    A21C2
}