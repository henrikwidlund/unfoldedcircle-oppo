namespace Oppo;

public enum OppoStreamingEventType : sbyte
{
    Unknown = 1,
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

public abstract record OppoStreamingEvent(in OppoStreamingEventType Type);

public sealed record OppoUnknownStreamingEvent(string RawData)
    : OppoStreamingEvent(OppoStreamingEventType.Unknown);

public sealed record OppoPowerStateStreamingEvent(in PowerState PowerState)
    : OppoStreamingEvent(OppoStreamingEventType.PowerState);

public sealed record OppoPlaybackStatusStreamingEvent(in PlaybackStatus PlaybackStatus)
    : OppoStreamingEvent(OppoStreamingEventType.PlaybackStatus);

public sealed record OppoVolumeStreamingEvent(in VolumeInfo VolumeInfo)
    : OppoStreamingEvent(OppoStreamingEventType.Volume);

public sealed record OppoDiscTypeStreamingEvent(in DiscType DiscType)
    : OppoStreamingEvent(OppoStreamingEventType.DiscType);

public sealed record OppoInputSourceStreamingEvent(in InputSource InputSource)
    : OppoStreamingEvent(OppoStreamingEventType.InputSource);

public sealed record OppoVideoResolutionStreamingEvent(in HDMIResolution Resolution)
    : OppoStreamingEvent(OppoStreamingEventType.HdmiResolution);

public sealed record OppoAudioTypeStreamingEvent(string AudioType)
    : OppoStreamingEvent(OppoStreamingEventType.AudioType);

public sealed record OppoSubtitleTypeStreamingEvent(string SubtitleType)
    : OppoStreamingEvent(OppoStreamingEventType.SubtitleType);

public sealed record OppoThreeDStatusStreamingEvent(in bool Is3D)
    : OppoStreamingEvent(OppoStreamingEventType.ThreeDStatus);

public sealed record OppoAspectRatioStreamingEvent(in AspectRatio AspectRatio)
    : OppoStreamingEvent(OppoStreamingEventType.AspectRatio);

public sealed record OppoPlaybackProgressStreamingEvent(
    in ushort Title,
    in ushort Chapter,
    in OppoTimeCodeType TimeCodeType,
    in uint Seconds)
    : OppoStreamingEvent(OppoStreamingEventType.PlaybackProgress);

