namespace Oppo;

internal static class Oppo20XCommand
{
    /// <summary>
    /// Toggle power STANDBY and ON
    /// </summary>
    public static readonly byte[] PowerToggle = "#POW\r"u8.ToArray();
    
    /// <summary>
    /// Discrete on
    /// </summary>
    public static readonly byte[] PowerOn = "#PON\r"u8.ToArray();
    
    /// <summary>
    /// Discrete off
    /// </summary>
    public static readonly byte[] PowerOff = "#POF\r"u8.ToArray();
    
    /// <summary>
    /// Open/close the disc tray
    /// </summary>
    public static readonly byte[] EjectToggle = "#EJT\r"u8.ToArray();
    
    /// <summary>
    /// Dim front panel display
    /// </summary>
    public static readonly byte[] Dimmer = "#DIM\r"u8.ToArray();
    
    /// <summary>
    /// Pure Audio mode (no video)
    /// </summary>
    public static readonly byte[] PureAudioToggle = "#PUR\r"u8.ToArray();
    
    /// <summary>
    /// Increase volume
    /// </summary>
    public static readonly byte[] VolumeUp = "#VUP\r"u8.ToArray();
    
    /// <summary>
    /// Decrease volume
    /// </summary>
    public static readonly byte[] VolumeDown = "#VDN\r"u8.ToArray();

    /// <summary>
    /// Mute audio
    /// </summary>
    public static readonly byte[] MuteToggle = "#MUT\r"u8.ToArray();
    
    /// <summary>
    /// Numeric key 1
    /// </summary>
    public static readonly byte[] NumericKey1 = "#NU1\r"u8.ToArray();
    
    /// <summary>
    /// Numeric key 2
    /// </summary>
    public static readonly byte[] NumericKey2 = "#NU2\r"u8.ToArray();
    
    /// <summary>
    /// Numeric key 3
    /// </summary>
    public static readonly byte[] NumericKey3 = "#NU3\r"u8.ToArray();
    
    /// <summary>
    /// Numeric key 4
    /// </summary>
    public static readonly byte[] NumericKey4 = "#NU4\r"u8.ToArray();
    
    /// <summary>
    /// Numeric key 5
    /// </summary>
    public static readonly byte[] NumericKey5 = "#NU5\r"u8.ToArray();
    
    /// <summary>
    /// Numeric key 6
    /// </summary>
    public static readonly byte[] NumericKey6 = "#NU6\r"u8.ToArray();
    
    /// <summary>
    /// Numeric key 7
    /// </summary>
    public static readonly byte[] NumericKey7 = "#NU7\r"u8.ToArray();
    
    /// <summary>
    /// Numeric key 8
    /// </summary>
    public static readonly byte[] NumericKey8 = "#NU8\r"u8.ToArray();
    
    /// <summary>
    /// Numeric key 9
    /// </summary>
    public static readonly byte[] NumericKey9 = "#NU9\r"u8.ToArray();
    
    /// <summary>
    /// Numeric key 0
    /// </summary>
    public static readonly byte[] NumericKey0 = "#NU0\r"u8.ToArray();
    
    /// <summary>
    /// Clear numeric input
    /// </summary>
    public static readonly byte[] Clear = "#CLR\r"u8.ToArray();
    
    /// <summary>
    /// Play from a specified location
    /// </summary>
    public static readonly byte[] GoTo = "#GOT\r"u8.ToArray();
    
    /// <summary>
    /// Go to Home Menu to select media source
    /// </summary>
    public static readonly byte[] Home = "#HOM\r"u8.ToArray();
    
    /// <summary>
    /// Show previous page
    /// </summary>
    public static readonly byte[] PageUp = "#PUP\r"u8.ToArray();
    
    /// <summary>
    /// Show next page
    /// </summary>
    public static readonly byte[] PageDown = "#PDN\r"u8.ToArray();
    
    /// <summary>
    /// Show/hide on-screen display
    /// </summary>
    public static readonly byte[] InfoToggle = "#OSD\r"u8.ToArray();
    
    /// <summary>
    /// Show BD top menu or DVD title menu
    /// </summary>
    public static readonly byte[] TopMenu = "#TTL\r"u8.ToArray();
    
    /// <summary>
    /// Show BD pop-up menu or DVD menu
    /// </summary>
    public static readonly byte[] PopUpMenu = "#MNU\r"u8.ToArray();
    
    /// <summary>
    /// Navigation
    /// </summary>
    public static readonly byte[] UpArrow = "#NUP\r"u8.ToArray();
    
    /// <summary>
    /// Navigation
    /// </summary>
    public static readonly byte[] LeftArrow = "#NLT\r"u8.ToArray();
    
    /// <summary>
    /// Navigation
    /// </summary>
    public static readonly byte[] RightArrow = "#NRT\r"u8.ToArray();
    
    /// <summary>
    /// Navigation
    /// </summary>
    public static readonly byte[] DownArrow = "#NDN\r"u8.ToArray();
    
    /// <summary>
    /// Navigation
    /// </summary>
    public static readonly byte[] Enter = "#SEL\r"u8.ToArray();
    
    /// <summary>
    /// Enter the player setup menu
    /// </summary>
    public static readonly byte[] Setup = "#SET\r"u8.ToArray();
    
    /// <summary>
    /// Return to the previous menu or mode
    /// </summary>
    public static readonly byte[] Return = "#RET\r"u8.ToArray();
    
    /// <summary>
    /// Function varies by content
    /// </summary>
    public static readonly byte[] Red = "#RED\r"u8.ToArray();
    
    /// <summary>
    /// Function varies by content
    /// </summary>
    public static readonly byte[] Green = "#GRN\r"u8.ToArray();
    
    /// <summary>
    /// Function varies by content
    /// </summary>
    public static readonly byte[] Blue = "#BLU\r"u8.ToArray();
    
    /// <summary>
    /// Function varies by content
    /// </summary>
    public static readonly byte[] Yellow = "#YLW\r"u8.ToArray();
    
    /// <summary>
    /// Stop playback
    /// </summary>
    public static readonly byte[] Stop = "#STP\r"u8.ToArray();
    
    /// <summary>
    /// Start playback
    /// </summary>
    public static readonly byte[] Play = "#PLA\r"u8.ToArray();
    
    /// <summary>
    /// Pause playback
    /// </summary>
    public static readonly byte[] Pause = "#PAU\r"u8.ToArray();
    
    /// <summary>
    /// Skip to previous
    /// </summary>
    public static readonly byte[] Previous = "#PRE\r"u8.ToArray();
    
    /// <summary>
    /// Fast reverse play
    /// </summary>
    public static readonly byte[] Reverse = "#REV\r"u8.ToArray();
    
    /// <summary>
    /// Fast forward play
    /// </summary>
    public static readonly byte[] Forward = "#FWD\r"u8.ToArray();
    
    /// <summary>
    /// Skip to next
    /// </summary>
    public static readonly byte[] Next = "#NXT\r"u8.ToArray();
    
    /// <summary>
    /// Change audio language or channel
    /// </summary>
    public static readonly byte[] Audio = "#AUD\r"u8.ToArray();
    
    /// <summary>
    /// Change subtitle language
    /// </summary>
    public static readonly byte[] Subtitle = "#SUB\r"u8.ToArray();
    
    /// <summary>
    /// Change camera angle
    /// </summary>
    public static readonly byte[] Angle = "#ANG\r"u8.ToArray();
    
    /// <summary>
    /// Zoom in/out and adjust aspect ratio
    /// </summary>
    public static readonly byte[] Zoom = "#ZOM\r"u8.ToArray();
    
    /// <summary>
    /// Turn on/off Secondary Audio Program
    /// </summary>
    public static readonly byte[] SecondaryAudioProgram = "#SAP\r"u8.ToArray();
    
    /// <summary>
    /// Repeat play the selected section
    /// </summary>
    // ReSharper disable InconsistentNaming
    public static readonly byte[] ABReplay = "#ATB\r"u8.ToArray();
    // ReSharper restore InconsistentNaming
    
    /// <summary>
    /// Repeat play
    /// </summary>
    public static readonly byte[] Repeat = "#RPT\r"u8.ToArray();
    
    /// <summary>
    /// Show/hide Picture-in-Picture
    /// </summary>
    public static readonly byte[] PictureInPicture = "#PIP\r"u8.ToArray();
    
    /// <summary>
    /// Switch output resolution
    /// </summary>
    public static readonly byte[] Resolution = "#HDM\r"u8.ToArray();
    
    /// <summary>
    /// Press and hold the SUBTITLE key. This activates the subtitle shift feature
    /// </summary>
    public static readonly byte[] SubtitleHold = "#SUH\r"u8.ToArray();
    
    /// <summary>
    /// Show/hide the Option menu
    /// </summary>
    public static readonly byte[] Option = "#OPT\r"u8.ToArray();
    
    /// <summary>
    /// Show/hide the 2D-to-3D Conversion or 3D adjustment menu
    /// </summary>
    public static readonly byte[] ThreeD = "#M3D\r"u8.ToArray();
    
    /// <summary>
    /// Display the Picture Adjustment menu
    /// </summary>
    public static readonly byte[] PictureAdjustment = "#SEH\r"u8.ToArray();
    
    /// <summary>
    /// Display the HDR selection menu
    /// </summary>
    // ReSharper disable InconsistentNaming
    public static readonly byte[] HDR = "#HDR\r"u8.ToArray();
    // ReSharper restore InconsistentNaming
    
    /// <summary>
    /// Show on-screen detailed information
    /// </summary>
    public static readonly byte[] InfoHold = "#INH\r"u8.ToArray();
    
    /// <summary>
    /// Set resolution to Auto (default)
    /// </summary>
    public static readonly byte[] ResolutionHold = "#RLH\r"u8.ToArray();
    
    /// <summary>
    /// Display the A/V Sync adjustment menu
    /// </summary>
    // ReSharper disable InconsistentNaming
    public static readonly byte[] AVSync = "#AVS\r"u8.ToArray();
    // ReSharper restore InconsistentNaming
    
    /// <summary>
    /// Gapless Play. This functions the same as selecting Gapless Play in the Option Menu.
    /// </summary>
    public static readonly byte[] GaplessPlay = "#GPA\r"u8.ToArray();
    
    /// <summary>
    /// No operation.
    /// </summary>
    public static readonly byte[] Noop = "#NOP\r"u8.ToArray();
    
    /// <summary>
    /// Display the Input menu. Input selection can be made with visual cursor, or by following the SRC command with a numeric key command (e.g. #SRC followed by #NU1)
    /// </summary>
    public static readonly byte[] Input = "#SRC\r"u8.ToArray();
}

internal static class Oppo20XQueryCommand
{
    /// <summary>
    /// Query verbose mode
    /// </summary>
    public static readonly byte[] QueryVerboseMode = "#QVM\r"u8.ToArray();
    
    /// <summary>
    /// Query power status
    /// </summary>
    public static readonly byte[] QueryPowerStatus = "#QPW\r"u8.ToArray();
    
    /// <summary>
    /// Query firmware version
    /// </summary>
    public static readonly byte[] QueryFirmwareVersion = "#QVR\r"u8.ToArray();
    
    /// <summary>
    /// Query volume
    /// </summary>
    public static readonly byte[] QueryVolume = "#QVL\r"u8.ToArray();
    
    /// <summary>
    /// Query HDMI Resolution
    /// </summary>
    // ReSharper disable InconsistentNaming
    public static readonly byte[] QueryHDMIResolution = "#QHD\r"u8.ToArray();
    // ReSharper restore InconsistentNaming
    
    /// <summary>
    /// Query playback status
    /// </summary>
    public static readonly byte[] QueryPlaybackStatus = "#QPL\r"u8.ToArray();
    
    /// <summary>
    /// Query Track/Title elapsed time
    /// </summary>
    public static readonly byte[] QueryTrackOrTitleElapsedTime = "#QTE\r"u8.ToArray();
    
    /// <summary>
    /// Query Track/Title remaining time
    /// </summary>
    public static readonly byte[] QueryTrackOrTitleRemainingTime = "#QTR\r"u8.ToArray();
    
    /// <summary>
    /// Query Chapter elapsed time
    /// </summary>
    public static readonly byte[] QueryChapterElapsedTime = "#QCE\r"u8.ToArray();
    
    /// <summary>
    /// Query Chapter remaining time
    /// </summary>
    public static readonly byte[] QueryChapterRemainingTime = "#QCR\r"u8.ToArray();
    
    /// <summary>
    /// Query Total elapsed time
    /// </summary>
    public static readonly byte[] QueryTotalElapsedTime = "#QEL\r"u8.ToArray();
    
    /// <summary>
    /// Query Total remaining time
    /// </summary>
    public static readonly byte[] QueryTotalRemainingTime = "#QRE\r"u8.ToArray();
    
    /// <summary>
    /// Query disc type
    /// </summary>
    public static readonly byte[] QueryDiscType = "#QDT\r"u8.ToArray();
    
    /// <summary>
    /// Query Repeat Mode
    /// </summary>
    public static readonly byte[] QueryRepeatMode = "#QRP\r"u8.ToArray();
    
    /// <summary>
    /// Query Input Source (Return the current selected input source)
    /// </summary>
    public static readonly byte[] QueryInputSource = "#QIS\r"u8.ToArray();
    
    /// <summary>
    /// Query CDDB number
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static readonly byte[] QueryCDDBNumber = "#QCD\r"u8.ToArray();
    
    /// <summary>
    /// Query track name
    /// </summary>
    public static readonly byte[] QueryTrackName = "#QTN\r"u8.ToArray();
    
    /// <summary>
    /// Query track album
    /// </summary>
    public static readonly byte[] QueryTrackAlbum = "#QTA\r"u8.ToArray();
    
    /// <summary>
    /// Query track performer
    /// </summary>
    public static readonly byte[] QueryTrackPerformer = "#QTP\r"u8.ToArray();
}

internal static class Oppo20XAdvancedCommand
{
    /// <summary>
    /// Set Verbose Mode to off
    /// </summary>
    public static readonly byte[] SetVerboseModeOff = "#SVM 0\r"u8.ToArray();
    
    /// <summary>
    /// Enable unsolicited status updates. Only major status changes are reported.
    /// </summary>
    public static readonly byte[] SetVerboseModeUnsolicitedStatusUpdates = "#SVM 2\r"u8.ToArray();
    
    /// <summary>
    /// Enable detailed status updates. When content is playing, the player sends out playback time updates every second.
    /// </summary>
    public static readonly byte[] SetVerboseModeDetailedStatus = "#SVM 3\r"u8.ToArray();
    
    public static readonly byte[] SetHDMIResolutionAuto = "#SHD AUTO\r"u8.ToArray();
    public static readonly byte[] SetHDMIResolutionSource = "#SHD SRC\r"u8.ToArray();
    public static readonly byte[] SetHDMIResolutionUHDAuto = "#SHD UHD_AUTO\r"u8.ToArray();
    public static readonly byte[] SetHDMIResolutionUHDUHD24 = "#SHD UHD24\r"u8.ToArray();
    public static readonly byte[] SetHDMIResolutionUHDUHD50 = "#SHD UHD50\r"u8.ToArray();
    public static readonly byte[] SetHDMIResolutionUHDUHD60 = "#SHD UHD60\r"u8.ToArray();
    public static readonly byte[] SetHDMIResolution1080PAuto = "#SHD 1080P_AUTO\r"u8.ToArray();
    public static readonly byte[] SetHDMIResolution1080P24 = "#SHD 1080P24\r"u8.ToArray();
    public static readonly byte[] SetHDMIResolution1080P50 = "#SHD 1080P50\r"u8.ToArray();
    public static readonly byte[] SetHDMIResolution1080P60 = "#SHD 1080P60\r"u8.ToArray();
    public static readonly byte[] SetHDMIResolution1080I50 = "#SHD 1080I50\r"u8.ToArray();
    public static readonly byte[] SetHDMIResolution1080I60 = "#SHD 1080I60\r"u8.ToArray();
    public static readonly byte[] SetHDMIResolution720P50 = "#SHD 720P50\r"u8.ToArray();
    public static readonly byte[] SetHDMIResolution720P60 = "#SHD 720P60\r"u8.ToArray();
    public static readonly byte[] SetHDMIResolution576P = "#SHD 576P\r"u8.ToArray();
    public static readonly byte[] SetHDMIResolution576I = "#SHD 576I\r"u8.ToArray();
    public static readonly byte[] SetHDMIResolution480P = "#SHD 480P\r"u8.ToArray();
    public static readonly byte[] SetHDMIResolution480I = "#SHD 480I\r"u8.ToArray();
    
    /// <summary>
    /// Repeat chapter
    /// </summary>
    public static readonly byte[] SetRepeatModeChapter = "#SRP CH\r"u8.ToArray();
    
    /// <summary>
    /// Repeat title or CD track
    /// </summary>
    public static readonly byte[] SetRepeatModeTitle = "#SRP TT\r"u8.ToArray();
    
    /// <summary>
    /// Repeat all
    /// </summary>
    public static readonly byte[] SetRepeatModeAll = "#SRP ALL\r"u8.ToArray();
    
    /// <summary>
    /// Repeat off
    /// </summary>
    public static readonly byte[] SetRepeatModeOff = "#SRP OFF\r"u8.ToArray();
    
    /// <summary>
    /// Shuffle
    /// </summary>
    public static readonly byte[] SetRepeatModeShuffle = "#SRP SHF\r"u8.ToArray();
    
    /// <summary>
    /// Random
    /// </summary>
    public static readonly byte[] SetRepeatModeRandom = "#SRP RND\r"u8.ToArray();
}
