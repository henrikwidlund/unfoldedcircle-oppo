namespace Oppo;

public enum OppoTimeCodeType : sbyte
{
    Unknown = 1,
    TotalElapsed,
    TotalRemaining,
    TitleElapsed,
    TitleRemaining,
    ChapterElapsed,
    ChapterRemaining
}

public abstract record OppoStreamingEvent;

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
