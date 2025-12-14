using System.ComponentModel.DataAnnotations;

using NetEscapades.EnumGenerators;

namespace UnfoldedCircle.OppoBluRay.Configuration;

[EnumExtensions(MetadataSource = MetadataSource.DisplayAttribute)]
internal enum OppoSensorType : sbyte
{
    [Display(Name = "Disc Type")]
    DiscType = 1,

    [Display(Name = "Input Source")]
    InputSource,

    [Display(Name = "HDMI Resolution")]
    HDMIResolution,

    [Display(Name = "Audio Type")]
    AudioType,

    [Display(Name = "Subtitle Type")]
    SubtitleType,

    [Display(Name = "3D Status")]
    ThreeDStatus,

    [Display(Name = "HDR Status")]
    HDRStatus,

    [Display(Name = "Aspect Ratio")]
    AspectRatio
}