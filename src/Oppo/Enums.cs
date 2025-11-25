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

public enum DiscType : sbyte
{
    // ReSharper disable InconsistentNaming
    BlueRayMovie = 1,
    DVDVideo,
    DVDAudio,
    SACD,
    CDDiscAudio,
    DataDisc,
    UltraHDBluRay,
    NoDisc,
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

public enum InputSource : sbyte
{
    // ReSharper disable InconsistentNaming
    Unknown,
    BluRayPlayer,
    
    // 20x models,
    HDMIIn,
    ARCHDMIOut,
    
    // 10x and 205 models
    Optical,
    Coaxial,
    USBAudio,
    
    // pre 20x models
    HDMIFront,
    HDMIBack,
    ARCHDMIOut1,
    ARCHDMIOut2
    // ReSharper restore InconsistentNaming
}