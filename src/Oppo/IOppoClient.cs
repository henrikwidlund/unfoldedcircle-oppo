using System.ComponentModel.DataAnnotations;

namespace Oppo;

/// <summary>
/// Interface for controlling an Oppo player.
/// </summary>
public interface IOppoClient : IDisposable
{
    /// <summary>
    /// Toggle power STANDBY and ON
    /// </summary>
    ValueTask<OppoResult<PowerState>> PowerToggleAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Discrete on
    /// </summary>
    ValueTask<OppoResult<PowerState>> PowerOnAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Discrete off
    /// </summary>
    ValueTask<OppoResult<PowerState>> PowerOffAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Open/close the disc tray
    /// </summary>
    ValueTask<OppoResult<TrayState>> EjectToggleAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Dim front panel display
    /// </summary>
    ValueTask<OppoResult<DimmerState>> DimmerAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Pure Audio mode (no video)
    /// </summary>
    ValueTask<OppoResult<PureAudioState>> PureAudioToggleAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Increase volume
    /// </summary>
    ValueTask<OppoResult<ushort?>> VolumeUpAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Decrease volume
    /// </summary>
    ValueTask<OppoResult<ushort?>> VolumeDownAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Mute audio
    /// </summary>
    ValueTask<OppoResult<MuteState>> MuteToggleAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Numeric key 1
    /// </summary>
    ValueTask<bool> NumericInputAsync([Range(0, 9)] ushort number, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Clear numeric input
    /// </summary>
    ValueTask<bool> ClearAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Play from a specified location
    /// </summary>
    ValueTask<bool> GoToAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Go to Home Menu to select media source
    /// </summary>
    ValueTask<bool> HomeAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Show previous page
    /// </summary>
    ValueTask<bool> PageUpAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Show next page
    /// </summary>
    ValueTask<bool> PageDownAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Show/hide on-screen display
    /// </summary>
    ValueTask<bool> InfoToggleAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Show BD top menu or DVD title menu
    /// </summary>
    ValueTask<bool> TopMenuAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Show BD pop-up menu or DVD menu
    /// </summary>
    ValueTask<bool> PopUpMenuAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Numeric key 0
    /// </summary>
    ValueTask<bool> UpArrowAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Navigation
    /// </summary>
    ValueTask<bool> LeftArrowAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Navigation
    /// </summary>
    ValueTask<bool> RightArrowAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Navigation
    /// </summary>
    ValueTask<bool> DownArrowAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Navigation
    /// </summary>
    ValueTask<bool> EnterAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Enter the player setup menu
    /// </summary>
    ValueTask<bool> SetupAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Return to the previous menu or mode
    /// </summary>
    ValueTask<bool> ReturnAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Function varies by content
    /// </summary>
    ValueTask<bool> RedAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Function varies by content
    /// </summary>
    ValueTask<bool> GreenAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Function varies by content
    /// </summary>
    ValueTask<bool> BlueAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Function varies by content
    /// </summary>
    ValueTask<bool> YellowAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Stop playback
    /// </summary>
    ValueTask<bool> StopAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Start playback
    /// </summary>
    ValueTask<bool> PlayAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Pause playback
    /// </summary>
    ValueTask<bool> PauseAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Skip to previous
    /// </summary>
    ValueTask<bool> PreviousAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Fast reverse play
    /// </summary>
    ValueTask<OppoResult<ushort?>> ReverseAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Fast forward play
    /// </summary>
    ValueTask<OppoResult<ushort?>> ForwardAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Skip to next
    /// </summary>
    ValueTask<bool> NextAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Change audio language or channel
    /// </summary>
    ValueTask<bool> AudioAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Change subtitle language
    /// </summary>
    ValueTask<bool> SubtitleAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Change camera angle
    /// </summary>
    ValueTask<OppoResult<string>> AngleAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Zoom in/out and adjust aspect ratio
    /// </summary>
    ValueTask<OppoResult<string>> ZoomAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Turn on/off Secondary Audio Program
    /// </summary>
    ValueTask<OppoResult<string>> SecondaryAudioProgramAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Repeat play the selected section
    /// </summary>
    // ReSharper disable InconsistentNaming
    ValueTask<OppoResult<ABReplayState>> ABReplayAsync(CancellationToken cancellationToken = default);
    // ReSharper restore InconsistentNaming
    
    /// <summary>
    /// Repeat play
    /// </summary>
    ValueTask<OppoResult<RepeatState>> RepeatAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Show/hide Picture-in-Picture
    /// </summary>
    ValueTask<OppoResult<string>> PictureInPictureAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Switch output resolution
    /// </summary>
    ValueTask<bool> ResolutionAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Press and hold the SUBTITLE key. This activates the subtitle shift feature
    /// </summary>
    ValueTask<bool> SubtitleHoldAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Show/hide the Option menu
    /// </summary>
    ValueTask<bool> OptionAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Show/hide the 2D-to-3D Conversion or 3D adjustment menu
    /// </summary>
    ValueTask<bool> ThreeDAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Display the Picture Adjustment menu
    /// </summary>
    ValueTask<bool> PictureAdjustmentAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Display the HDR selection menu
    /// </summary>
    // ReSharper disable InconsistentNaming
    ValueTask<bool> HDRAsync(CancellationToken cancellationToken = default);
    // ReSharper restore InconsistentNaming
    
    /// <summary>
    /// Show on-screen detailed information
    /// </summary>
    ValueTask<bool> InfoHoldAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Set resolution to Auto (default)
    /// </summary>
    ValueTask<bool> ResolutionHoldAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Display the A/V Sync adjustment menu
    /// </summary>
    // ReSharper disable InconsistentNaming
    ValueTask<bool> AVSyncAsync(CancellationToken cancellationToken = default);
    // ReSharper restore InconsistentNaming
    
    /// <summary>
    /// Gapless Play. This functions the same as selecting Gapless Play in the Option Menu.
    /// </summary>
    ValueTask<bool> GaplessPlayAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// No operation.
    /// </summary>
    ValueTask<bool> NoopAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Display the Input menu. Input selection can be made with visual cursor, or by following the SRC command with a numeric key command (e.g. #SRC followed by #NU1)
    /// </summary>
    ValueTask<bool> InputAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Set the repeat mode.
    /// </summary>
    ValueTask<OppoResult<RepeatMode>> SetRepeatAsync(RepeatMode mode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Set volume control
    /// </summary>
    ValueTask<OppoResult<ushort>> SetVolumeAsync([Range(0, 100)] ushort volume, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Query volume
    /// </summary>
    ValueTask<OppoResult<VolumeInfo>> QueryVolumeAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Query power status
    /// </summary>
    ValueTask<OppoResult<PowerState>> QueryPowerStatusAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Query playback status
    /// </summary>
    ValueTask<OppoResult<PlaybackStatus>> QueryPlaybackStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Query HDMI resolution
    /// </summary>
    ValueTask<OppoResult<HDMIResolution>> QueryHDMIResolutionAsync(CancellationToken cancellationToken = default);


    /// <summary>
    /// Query Track/Title elapsed time
    /// </summary>
    ValueTask<OppoResult<uint>> QueryTrackOrTitleElapsedTimeAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Query Track/Title remaining time
    /// </summary>
    ValueTask<OppoResult<uint>> QueryTrackOrTitleRemainingTimeAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Query Chapter elapsed time
    /// </summary>
    ValueTask<OppoResult<uint>> QueryChapterElapsedTimeAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Query Chapter remaining time
    /// </summary>
    ValueTask<OppoResult<uint>> QueryChapterRemainingTimeAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Query Total elapsed time 
    /// </summary>
    ValueTask<OppoResult<uint>> QueryTotalElapsedTimeAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Query Total remaining time 
    /// </summary>
    ValueTask<OppoResult<uint>> QueryTotalRemainingTimeAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Query disc type
    /// </summary>
    ValueTask<OppoResult<DiscType>> QueryDiscTypeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Query audio type
    /// </summary>
    ValueTask<OppoResult<string>> QueryAudioTypeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Query subtitle type
    /// </summary>
    ValueTask<OppoResult<string>> QuerySubtitleTypeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Query 3D Status
    /// </summary>
    ValueTask<OppoResult<bool>> QueryThreeDStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Query HDR Status
    /// </summary>
    ValueTask<OppoResult<HDRStatus>> QueryHDRStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Query aspect ratio setting
    /// </summary>
    ValueTask<OppoResult<AspectRatio>> QueryAspectRatioAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Query Repeat Mode
    /// </summary>
    ValueTask<OppoResult<CurrentRepeatMode>> QueryRepeatModeAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Query Input Source (Return the current selected input source)
    /// </summary>
    ValueTask<OppoResult<InputSource>> QueryInputSourceAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Query Input Source (Return the current selected input source)
    /// </summary>
    ValueTask<OppoResult<InputSource>> SetInputSourceAsync(InputSource inputSource, CancellationToken cancellationToken = default);

    /// <summary>
    /// Query CDDB number
    /// </summary>
    // ReSharper disable once InconsistentNaming
    ValueTask<OppoResult<string>> QueryCDDBNumberAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Query track name
    /// </summary>
    ValueTask<OppoResult<string>> QueryTrackNameAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Query track album
    /// </summary>
    ValueTask<OppoResult<string>> QueryTrackAlbumAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Query track performer
    /// </summary>
    ValueTask<OppoResult<string>> QueryTrackPerformerAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Set verbose mode.
    /// </summary>
    ValueTask<OppoResult<VerboseMode>> SetVerboseMode(VerboseMode verboseMode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if the client is connected.
    /// </summary>
    ValueTask<bool> IsConnectedAsync(TimeSpan? timeout = null);
    
    /// <summary>
    /// Get the host or IP address of the client.
    /// </summary>
    string HostName { get; }
}