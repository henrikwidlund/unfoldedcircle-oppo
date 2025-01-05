namespace Oppo;

public enum PowerState
{
    On = 1,
    Off,
    Unknown
}

public enum TrayState
{
    Open = 1,
    Closed,
    Unknown
}

public enum DimmerState
{
    On = 1,
    Dim,
    Off,
    Unknown
}

public enum PureAudioState
{
    On = 1,
    Off,
    Unknown
}

public enum MuteState
{
    On = 1,
    Off,
    Unknown
}

// ReSharper disable InconsistentNaming
public enum ABReplayState
{
    A = 1,
    AB,
    Off,
    Unknown
}
// ReSharper restore InconsistentNaming

public enum RepeatState
{
    RepeatChapter = 1,
    RepeatTitle,
    Off,
    Unknown
}

public enum RepeatMode
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

public enum PlaybackStatus
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

public enum DiscType
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

public enum CurrentRepeatMode
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

public enum OppoModel
{
    // ReSharper disable InconsistentNaming
    BDP8395,
    BDP10X,
    UDP203,
    UDP205
    // ReSharper restore InconsistentNaming
}

public enum InputSource
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