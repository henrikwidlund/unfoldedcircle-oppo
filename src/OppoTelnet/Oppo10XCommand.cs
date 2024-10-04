namespace OppoTelnet;

internal static class Oppo10XCommand
{
    /// <summary>
    /// Toggle power STANDBY and ON
    /// </summary>
    public static readonly byte[] PowerToggle = "REMOTE POW"u8.ToArray();
    
    /// <summary>
    /// Discrete on
    /// </summary>
    public static readonly byte[] PowerOn = "REMOTE PON"u8.ToArray();
    
    /// <summary>
    /// Discrete off
    /// </summary>
    public static readonly byte[] PowerOff = "REMOTE POF"u8.ToArray();
    
    /// <summary>
    /// Open/close the disc tray
    /// </summary>
    public static readonly byte[] EjectToggle = "REMOTE EJT"u8.ToArray();
    
    /// <summary>
    /// Dim front panel display
    /// </summary>
    public static readonly byte[] Dimmer = "REMOTE DIM"u8.ToArray();
    
    /// <summary>
    /// Pure Audio mode (no video)
    /// </summary>
    public static readonly byte[] PureAudioToggle = "REMOTE PUR"u8.ToArray();
    
    /// <summary>
    /// Increase volume
    /// </summary>
    public static readonly byte[] VolumeUp = "REMOTE VUP"u8.ToArray();
    
    /// <summary>
    /// Decrease volume
    /// </summary>
    public static readonly byte[] VolumeDown = "REMOTE VDN"u8.ToArray();

    /// <summary>
    /// Mute audio
    /// </summary>
    public static readonly byte[] MuteToggle = "REMOTE MUT"u8.ToArray();
    
    /// <summary>
    /// Numeric key 1
    /// </summary>
    public static readonly byte[] NumericKey1 = "REMOTE NU1"u8.ToArray();
    
    /// <summary>
    /// Numeric key 2
    /// </summary>
    public static readonly byte[] NumericKey2 = "REMOTE NU2"u8.ToArray();
    
    /// <summary>
    /// Numeric key 3
    /// </summary>
    public static readonly byte[] NumericKey3 = "REMOTE NU3"u8.ToArray();
    
    /// <summary>
    /// Numeric key 4
    /// </summary>
    public static readonly byte[] NumericKey4 = "REMOTE NU4"u8.ToArray();
    
    /// <summary>
    /// Numeric key 5
    /// </summary>
    public static readonly byte[] NumericKey5 = "REMOTE NU5"u8.ToArray();
    
    /// <summary>
    /// Numeric key 6
    /// </summary>
    public static readonly byte[] NumericKey6 = "REMOTE NU6"u8.ToArray();
    
    /// <summary>
    /// Numeric key 7
    /// </summary>
    public static readonly byte[] NumericKey7 = "REMOTE NU7"u8.ToArray();
    
    /// <summary>
    /// Numeric key 8
    /// </summary>
    public static readonly byte[] NumericKey8 = "REMOTE NU8"u8.ToArray();
    
    /// <summary>
    /// Numeric key 9
    /// </summary>
    public static readonly byte[] NumericKey9 = "REMOTE NU9"u8.ToArray();
    
    /// <summary>
    /// Numeric key 0
    /// </summary>
    public static readonly byte[] NumericKey0 = "REMOTE NU0"u8.ToArray();
    
    /// <summary>
    /// Clear numeric input
    /// </summary>
    public static readonly byte[] Clear = "REMOTE CLR"u8.ToArray();
    
    /// <summary>
    /// Play from a specified location
    /// </summary>
    public static readonly byte[] GoTo = "REMOTE GOT"u8.ToArray();
    
    /// <summary>
    /// Go to Home Menu to select media source
    /// </summary>
    public static readonly byte[] Home = "REMOTE HOM"u8.ToArray();
    
    /// <summary>
    /// Show previous page
    /// </summary>
    public static readonly byte[] PageUp = "REMOTE PUP"u8.ToArray();
    
    /// <summary>
    /// Show next page
    /// </summary>
    public static readonly byte[] PageDown = "REMOTE PDN"u8.ToArray();
    
    /// <summary>
    /// Show/hide on-screen display
    /// </summary>
    public static readonly byte[] InfoToggle = "REMOTE OSD"u8.ToArray();
    
    /// <summary>
    /// Show BD top menu or DVD title menu
    /// </summary>
    public static readonly byte[] TopMenu = "REMOTE TTL"u8.ToArray();
    
    /// <summary>
    /// Show BD pop-up menu or DVD menu
    /// </summary>
    public static readonly byte[] PopUpMenu = "REMOTE MNU"u8.ToArray();
    
    /// <summary>
    /// Navigation
    /// </summary>
    public static readonly byte[] UpArrow = "REMOTE NUP"u8.ToArray();
    
    /// <summary>
    /// Navigation
    /// </summary>
    public static readonly byte[] LeftArrow = "REMOTE NLT"u8.ToArray();
    
    /// <summary>
    /// Navigation
    /// </summary>
    public static readonly byte[] RightArrow = "REMOTE NRT"u8.ToArray();
    
    /// <summary>
    /// Navigation
    /// </summary>
    public static readonly byte[] DownArrow = "REMOTE NDN"u8.ToArray();
    
    /// <summary>
    /// Navigation
    /// </summary>
    public static readonly byte[] Enter = "REMOTE SEL"u8.ToArray();
    
    /// <summary>
    /// Enter the player setup menu
    /// </summary>
    public static readonly byte[] Setup = "REMOTE SET"u8.ToArray();
    
    /// <summary>
    /// Return to the previous menu or mode
    /// </summary>
    public static readonly byte[] Return = "REMOTE RET"u8.ToArray();
    
    /// <summary>
    /// Function varies by content
    /// </summary>
    public static readonly byte[] Red = "REMOTE RED"u8.ToArray();
    
    /// <summary>
    /// Function varies by content
    /// </summary>
    public static readonly byte[] Green = "REMOTE GRN"u8.ToArray();
    
    /// <summary>
    /// Function varies by content
    /// </summary>
    public static readonly byte[] Blue = "REMOTE BLU"u8.ToArray();
    
    /// <summary>
    /// Function varies by content
    /// </summary>
    public static readonly byte[] Yellow = "REMOTE YLW"u8.ToArray();
    
    /// <summary>
    /// Stop playback
    /// </summary>
    public static readonly byte[] Stop = "REMOTE STP"u8.ToArray();
    
    /// <summary>
    /// Start playback
    /// </summary>
    public static readonly byte[] Play = "REMOTE PLA"u8.ToArray();
    
    /// <summary>
    /// Pause playback
    /// </summary>
    public static readonly byte[] Pause = "REMOTE PAU"u8.ToArray();
    
    /// <summary>
    /// Skip to previous
    /// </summary>
    public static readonly byte[] Previous = "REMOTE PRE"u8.ToArray();
    
    /// <summary>
    /// Fast reverse play
    /// </summary>
    public static readonly byte[] Reverse = "REMOTE REV"u8.ToArray();
    
    /// <summary>
    /// Fast forward play
    /// </summary>
    public static readonly byte[] Forward = "REMOTE FWD"u8.ToArray();
    
    /// <summary>
    /// Skip to next
    /// </summary>
    public static readonly byte[] Next = "REMOTE NXT"u8.ToArray();
    
    /// <summary>
    /// Change audio language or channel
    /// </summary>
    public static readonly byte[] Audio = "REMOTE AUD"u8.ToArray();
    
    /// <summary>
    /// Change subtitle language
    /// </summary>
    public static readonly byte[] Subtitle = "REMOTE SUB"u8.ToArray();
    
    /// <summary>
    /// Change camera angle
    /// </summary>
    public static readonly byte[] Angle = "REMOTE ANG"u8.ToArray();
    
    /// <summary>
    /// Zoom in/out and adjust aspect ratio
    /// </summary>
    public static readonly byte[] Zoom = "REMOTE ZOM"u8.ToArray();
    
    /// <summary>
    /// Turn on/off Secondary Audio Program
    /// </summary>
    public static readonly byte[] SecondaryAudioProgram = "REMOTE SAP"u8.ToArray();
    
    /// <summary>
    /// Repeat play the selected section
    /// </summary>
    // ReSharper disable InconsistentNaming
    public static readonly byte[] ABReplay = "REMOTE ATB"u8.ToArray();
    // ReSharper restore InconsistentNaming
    
    /// <summary>
    /// Repeat play
    /// </summary>
    public static readonly byte[] Repeat = "REMOTE RPT"u8.ToArray();
    
    /// <summary>
    /// Show/hide Picture-in-Picture
    /// </summary>
    public static readonly byte[] PictureInPicture = "REMOTE PIP"u8.ToArray();
    
    /// <summary>
    /// Switch output resolution
    /// </summary>
    public static readonly byte[] Resolution = "REMOTE HDM"u8.ToArray();
    
    /// <summary>
    /// Press and hold the SUBTITLE key. This activates the subtitle shift feature
    /// </summary>
    public static readonly byte[] SubtitleHold = "REMOTE SUH"u8.ToArray();
    
    /// <summary>
    /// Show/hide the Option menu
    /// </summary>
    public static readonly byte[] Option = "REMOTE OPT"u8.ToArray();
    
    /// <summary>
    /// Show/hide the 2D-to-3D Conversion or 3D adjustment menu
    /// </summary>
    public static readonly byte[] ThreeD = "REMOTE M3D"u8.ToArray();
    
    /// <summary>
    /// Display the Picture Adjustment menu
    /// </summary>
    public static readonly byte[] PictureAdjustment = "REMOTE SEH"u8.ToArray();
    
    /// <summary>
    /// No operation.
    /// </summary>
    public static readonly byte[] Noop = "REMOTE NOP"u8.ToArray();
    
    /// <summary>
    /// Display the Input menu. Input selection can be made with visual cursor, or by following the SRC command with a numeric key command (e.g. REMOTE SRC followed by REMOTE NU1)
    /// </summary>
    public static readonly byte[] Input = "REMOTE SRC"u8.ToArray();
}

internal static class Oppo10XQueryCommand
{
    /// <summary>
    /// Query verbose mode
    /// </summary>
    public static readonly byte[] QueryVerboseMode = "REMOTE QVM"u8.ToArray();
    
    /// <summary>
    /// Query power status
    /// </summary>
    public static readonly byte[] QueryPowerStatus = "REMOTE QPW"u8.ToArray();
    
    /// <summary>
    /// Query firmware version
    /// </summary>
    public static readonly byte[] QueryFirmwareVersion = "REMOTE QVR"u8.ToArray();
    
    /// <summary>
    /// Query volume
    /// </summary>
    public static readonly byte[] QueryVolume = "REMOTE QVL"u8.ToArray();
    
    /// <summary>
    /// Query HDMI Resolution
    /// </summary>
    // ReSharper disable InconsistentNaming
    public static readonly byte[] QueryHDMIResolution = "REMOTE QHD"u8.ToArray();
    // ReSharper restore InconsistentNaming
    
    /// <summary>
    /// Query playback status
    /// </summary>
    public static readonly byte[] QueryPlaybackStatus = "REMOTE QPL"u8.ToArray();
    
    /// <summary>
    /// Query Track/Title elapsed time
    /// </summary>
    public static readonly byte[] QueryTrackOrTitleElapsedTime = "REMOTE QTE"u8.ToArray();
    
    /// <summary>
    /// Query Track/Title remaining time
    /// </summary>
    public static readonly byte[] QueryTrackOrTitleRemainingTime = "REMOTE QTR"u8.ToArray();
    
    /// <summary>
    /// Query Chapter elapsed time
    /// </summary>
    public static readonly byte[] QueryChapterElapsedTime = "REMOTE QCE"u8.ToArray();
    
    /// <summary>
    /// Query Chapter remaining time
    /// </summary>
    public static readonly byte[] QueryChapterRemainingTime = "REMOTE QCR"u8.ToArray();
    
    /// <summary>
    /// Query Total elapsed time
    /// </summary>
    public static readonly byte[] QueryTotalElapsedTime = "REMOTE QEL"u8.ToArray();
    
    /// <summary>
    /// Query Total remaining time
    /// </summary>
    public static readonly byte[] QueryTotalRemainingTime = "REMOTE QRE"u8.ToArray();
    
    /// <summary>
    /// Query disc type
    /// </summary>
    public static readonly byte[] QueryDiscType = "REMOTE QDT"u8.ToArray();
    
    /// <summary>
    /// Query Repeat Mode
    /// </summary>
    public static readonly byte[] QueryRepeatMode = "REMOTE QRP"u8.ToArray();
}

internal static class Oppo10XAdvancedCommand
{
    /// <summary>
    /// Set Verbose Mode to off
    /// </summary>
    public static readonly byte[] SetVerboseModeOff = "REMOTE SVM 0"u8.ToArray();
    
    /// <summary>
    /// Enable unsolicited status updates. Only major status changes are reported.
    /// </summary>
    public static readonly byte[] SetVerboseModeUnsolicitedStatusUpdates = "REMOTE SVM 2"u8.ToArray();
    
    /// <summary>
    /// Enable detailed status updates. When content is playing, the player sends out playback time updates every second.
    /// </summary>
    public static readonly byte[] SetVerboseModeDetailedStatus = "REMOTE SVM 3"u8.ToArray();
    
    public static readonly byte[] SetHDMIResolutionSDI = "REMOTE SHD SDI"u8.ToArray();
    public static readonly byte[] SetHDMIResolutionSDP = "REMOTE SHD SDP"u8.ToArray();
    public static readonly byte[] SetHDMIResolution720P = "REMOTE SHD 720P"u8.ToArray();
    public static readonly byte[] SetHDMIResolution1080I = "REMOTE SHD 1080I"u8.ToArray();
    public static readonly byte[] SetHDMIResolution1080P = "REMOTE SHD 1080P"u8.ToArray();
    public static readonly byte[] SetHDMIResolutionSource = "REMOTE SHD SRC"u8.ToArray();
    public static readonly byte[] SetHDMIResolutionAuto = "REMOTE SHD AUTO"u8.ToArray();
    
    /// <summary>
    /// Repeat chapter
    /// </summary>
    public static readonly byte[] SetRepeatModeChapter = "REMOTE SRP CH"u8.ToArray();
    
    /// <summary>
    /// Repeat title or CD track
    /// </summary>
    public static readonly byte[] SetRepeatModeTitle = "REMOTE SRP TT"u8.ToArray();
    
    /// <summary>
    /// Repeat all
    /// </summary>
    public static readonly byte[] SetRepeatModeAll = "REMOTE SRP ALL"u8.ToArray();
    
    /// <summary>
    /// Repeat off
    /// </summary>
    public static readonly byte[] SetRepeatModeOff = "REMOTE SRP OFF"u8.ToArray();
    
    /// <summary>
    /// Shuffle
    /// </summary>
    public static readonly byte[] SetRepeatModeShuffle = "REMOTE SRP SHF"u8.ToArray();
    
    /// <summary>
    /// Random
    /// </summary>
    public static readonly byte[] SetRepeatModeRandom = "REMOTE SRP RND"u8.ToArray();
}
