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

public sealed record OppoPowerStateStreamingEvent(in PowerState PowerState)
    : OppoStreamingEvent;

public sealed record OppoPlaybackStatusStreamingEvent(in PlaybackStatus PlaybackStatus)
    : OppoStreamingEvent;

public sealed record OppoVolumeStreamingEvent(in VolumeInfo VolumeInfo)
    : OppoStreamingEvent;

// ReSharper disable once NotAccessedPositionalProperty.Global
public sealed record OppoDiscTypeStreamingEvent(in DiscType DiscType)
    : OppoStreamingEvent;

// ReSharper disable once NotAccessedPositionalProperty.Global
public sealed record OppoInputSourceStreamingEvent(in InputSource InputSource)
    : OppoStreamingEvent;

public sealed record OppoVideoResolutionStreamingEvent(in HDMIResolution Resolution)
    : OppoStreamingEvent;

public sealed record OppoAudioTypeStreamingEvent(string AudioType)
    : OppoStreamingEvent;

public sealed record OppoSubtitleTypeStreamingEvent(string SubtitleType)
    : OppoStreamingEvent;

public sealed record OppoThreeDStatusStreamingEvent(in bool Is3D)
    : OppoStreamingEvent;

public sealed record OppoAspectRatioStreamingEvent(in AspectRatio AspectRatio)
    : OppoStreamingEvent;

public sealed record OppoPlaybackProgressStreamingEvent(
    in ushort Title,
    in ushort Chapter,
    in OppoTimeCodeType TimeCodeType,
    in uint Seconds)
    : OppoStreamingEvent;

