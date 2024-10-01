namespace OppoTelnet;

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
    DiscMenu
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
    UnknownDisc
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

public enum OppoModel : ushort
{
    // ReSharper disable InconsistentNaming
    BDP83 = 19999,
    BDP10X = 48360,
    UDP20X = 23
    // ReSharper restore InconsistentNaming
}