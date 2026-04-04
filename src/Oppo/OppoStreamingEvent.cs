using System.Buffers;

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

public abstract record OppoStreamingEvent(OppoStreamingEventType Type, in ReadOnlySequence<byte> RawValue);

public sealed record OppoUnknownStreamingEvent(in ReadOnlySequence<byte> RawValue)
    : OppoStreamingEvent(OppoStreamingEventType.Unknown, RawValue);

public sealed record OppoPowerStateStreamingEvent(PowerState PowerState, in ReadOnlySequence<byte> RawValue)
    : OppoStreamingEvent(OppoStreamingEventType.PowerState, RawValue);

public sealed record OppoPlaybackStatusStreamingEvent(PlaybackStatus PlaybackStatus, in ReadOnlySequence<byte> RawValue)
    : OppoStreamingEvent(OppoStreamingEventType.PlaybackStatus, RawValue);

public sealed record OppoVolumeStreamingEvent(VolumeInfo VolumeInfo, in ReadOnlySequence<byte> RawValue)
    : OppoStreamingEvent(OppoStreamingEventType.Volume, RawValue);

public sealed record OppoDiscTypeStreamingEvent(DiscType DiscType, in ReadOnlySequence<byte> RawValue)
    : OppoStreamingEvent(OppoStreamingEventType.DiscType, RawValue);

public sealed record OppoInputSourceStreamingEvent(InputSource InputSource, in ReadOnlySequence<byte> RawValue)
    : OppoStreamingEvent(OppoStreamingEventType.InputSource, RawValue);

public sealed record OppoVideoResolutionStreamingEvent(HDMIResolution Resolution, in ReadOnlySequence<byte> RawValue)
    : OppoStreamingEvent(OppoStreamingEventType.HdmiResolution, RawValue);

public sealed record OppoAudioTypeStreamingEvent(string AudioType, in ReadOnlySequence<byte> RawValue)
    : OppoStreamingEvent(OppoStreamingEventType.AudioType, RawValue);

public sealed record OppoSubtitleTypeStreamingEvent(string SubtitleType, in ReadOnlySequence<byte> RawValue)
    : OppoStreamingEvent(OppoStreamingEventType.SubtitleType, RawValue);

public sealed record OppoThreeDStatusStreamingEvent(bool Is3D, in ReadOnlySequence<byte> RawValue)
    : OppoStreamingEvent(OppoStreamingEventType.ThreeDStatus, RawValue);

public sealed record OppoAspectRatioStreamingEvent(AspectRatio AspectRatio, in ReadOnlySequence<byte> RawValue)
    : OppoStreamingEvent(OppoStreamingEventType.AspectRatio, RawValue);

public sealed record OppoPlaybackProgressStreamingEvent(
    ushort Title,
    ushort Chapter,
    OppoTimeCodeType TimeCodeType,
    uint Seconds,
    in ReadOnlySequence<byte> RawValue)
    : OppoStreamingEvent(OppoStreamingEventType.PlaybackProgress, RawValue);

