namespace Oppo;

public enum OppoTimeCodeType : byte
{
    Unknown = 1,
    TotalElapsed,
    TotalRemaining,
    TitleElapsed,
    TitleRemaining,
    ChapterElapsed,
    ChapterRemaining
}

public closed record OppoStreamingEvent;

public sealed record OppoUnknownStreamingEvent
    : OppoStreamingEvent;

public sealed record OppoPowerStateStreamingEvent(PowerState PowerState)
    : OppoStreamingEvent;

public sealed record OppoPlaybackStatusStreamingEvent(PlaybackStatus PlaybackStatus)
    : OppoStreamingEvent;

public sealed record OppoVolumeStreamingEvent(VolumeInfo VolumeInfo)
    : OppoStreamingEvent;

// ReSharper disable once NotAccessedPositionalProperty.Global
public sealed record OppoDiscTypeStreamingEvent(DiscType DiscType)
    : OppoStreamingEvent;

// ReSharper disable once NotAccessedPositionalProperty.Global
public sealed record OppoInputSourceStreamingEvent(InputSource InputSource)
    : OppoStreamingEvent;

public sealed record OppoVideoResolutionStreamingEvent(HDMIResolution Resolution)
    : OppoStreamingEvent;

public sealed record OppoAudioTypeStreamingEvent(string AudioType)
    : OppoStreamingEvent;

public sealed record OppoSubtitleTypeStreamingEvent(string SubtitleType)
    : OppoStreamingEvent;

public sealed record OppoThreeDStatusStreamingEvent(bool Is3D)
    : OppoStreamingEvent;

public sealed record OppoAspectRatioStreamingEvent(AspectRatio AspectRatio)
    : OppoStreamingEvent;

public sealed record OppoPlaybackProgressStreamingEvent(
    ushort Title,
    ushort Chapter,
    OppoTimeCodeType TimeCodeType,
    uint Seconds)
    : OppoStreamingEvent;

/// <summary>
/// Self-contained now-playing snapshot pushed by a Magnetar player (<c>UpdatePlayState</c>).
/// Field presence depends on <see cref="MediaType"/>: cd -&gt; TrackTitle; sacd -&gt; + DiscArtist/DiscTitle;
/// bd/vcd/dvd/video -&gt; FileName/Hdr/FourK/FrameRate; audio -&gt; Artist/Title/FileName.
/// </summary>
public sealed record OppoMagnetarPlayStateStreamingEvent(
    string MediaType,
    string State,
    string CurrTime,
    string TotalTime,
    string RepeatMode,
    string? TrackTitle,
    string? DiscArtist,
    string? DiscTitle,
    string? FileName,
    string? Artist,
    string? Title,
    string? Hdr,
    string? FourK,
    string? FrameRate)
    : OppoStreamingEvent;

/// <summary>
/// Synthetic power-off marker - not something the real player ever sends (it has no power-off push
/// at all, it just stops sending anything). Only ever produced when running through oppo-multiplexer:
/// the proxy synthesizes a <c>SyntheticPowerOff</c> cmd on backend disconnect so a client can tell
/// "player is off" apart from "proxy is still here, player just isn't talking" - its own socket to the
/// proxy never drops just because the proxy's connection to the real player did.
/// </summary>
public sealed record OppoMagnetarPowerOffStreamingEvent
    : OppoStreamingEvent;
