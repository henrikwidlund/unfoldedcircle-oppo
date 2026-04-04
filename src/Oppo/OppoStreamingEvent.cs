namespace Oppo;

public enum OppoStreamingEventType : byte
{
    Unknown,
    PowerState,
    PlaybackStatus,
    Volume,
    DiscType,
    InputSource,
    HdmiResolution,
    AudioType,
    SubtitleType,
    ThreeDStatus,
    AspectRatio,
    PlaybackProgress
}

public enum OppoTimeCodeType : byte
{
    Unknown,
    TotalElapsed,
    TotalRemaining,
    TitleElapsed,
    TitleRemaining,
    ChapterElapsed,
    ChapterRemaining
}

public abstract record OppoStreamingEvent(OppoStreamingEventType Type);

public sealed record OppoUnknownStreamingEvent(string RawData)
    : OppoStreamingEvent(OppoStreamingEventType.Unknown);

public sealed record OppoPowerStateStreamingEvent(PowerState PowerState)
    : OppoStreamingEvent(OppoStreamingEventType.PowerState);

public sealed record OppoPlaybackStatusStreamingEvent(PlaybackStatus PlaybackStatus)
    : OppoStreamingEvent(OppoStreamingEventType.PlaybackStatus);

public sealed record OppoVolumeStreamingEvent(VolumeInfo VolumeInfo)
    : OppoStreamingEvent(OppoStreamingEventType.Volume);

public sealed record OppoDiscTypeStreamingEvent(DiscType DiscType)
    : OppoStreamingEvent(OppoStreamingEventType.DiscType);

public sealed record OppoInputSourceStreamingEvent(InputSource InputSource)
    : OppoStreamingEvent(OppoStreamingEventType.InputSource);

public sealed record OppoVideoResolutionStreamingEvent(HDMIResolution Resolution)
    : OppoStreamingEvent(OppoStreamingEventType.HdmiResolution);

public sealed record OppoAudioTypeStreamingEvent(string AudioType)
    : OppoStreamingEvent(OppoStreamingEventType.AudioType);

public sealed record OppoSubtitleTypeStreamingEvent(string SubtitleType)
    : OppoStreamingEvent(OppoStreamingEventType.SubtitleType);

public sealed record OppoThreeDStatusStreamingEvent(bool Is3D)
    : OppoStreamingEvent(OppoStreamingEventType.ThreeDStatus);

public sealed record OppoAspectRatioStreamingEvent(AspectRatio AspectRatio)
    : OppoStreamingEvent(OppoStreamingEventType.AspectRatio);

public sealed record OppoPlaybackProgressStreamingEvent(
    ushort Title,
    ushort Chapter,
    OppoTimeCodeType TimeCodeType,
    uint Seconds)
    : OppoStreamingEvent(OppoStreamingEventType.PlaybackProgress);

