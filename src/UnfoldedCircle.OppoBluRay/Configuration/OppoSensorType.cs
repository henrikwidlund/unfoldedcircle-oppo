using NetEscapades.EnumGenerators;

namespace UnfoldedCircle.OppoBluRay.Configuration;

[EnumExtensions]
internal enum OppoSensorType : sbyte
{
    DiscType = 1,
    InputSource,
    HDMIResolution,
    AudioType,
    SubTitleType,
    ThreeDStatus,
    HDRStatus,
    AspectRatio,
    MediaFileFormat
}