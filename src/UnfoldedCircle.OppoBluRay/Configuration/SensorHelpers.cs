using System.Collections.Frozen;

using Oppo;

namespace UnfoldedCircle.OppoBluRay.Configuration;

internal static class SensorHelpers
{
    extension(OppoSensorType sensorType)
    {
        public string GetOppoSensorTypeName() =>
            sensorType switch
            {
                OppoSensorType.DiscType => "Disc Type",
                OppoSensorType.InputSource => "Input Source",
                OppoSensorType.HDMIResolution => "HDMI Resolution",
                OppoSensorType.AudioType => "Audio Type",
                OppoSensorType.SubTitleType => "Subtitle Type",
                OppoSensorType.ThreeDStatus => "3D Status",
                OppoSensorType.HDRStatus => "HDR Status",
                OppoSensorType.AspectRatio => "Aspect Ratio",
                OppoSensorType.MediaFileFormat => "Media File Format",
                _ => "Unknown"
            };
    }

    private static readonly FrozenSet<OppoSensorType> AllSensorTypes = OppoSensorTypeExtensions.GetValues().ToFrozenSet();

    private static readonly FrozenSet<OppoSensorType> Non20XSensorTypes = (new[]
    {
        OppoSensorType.DiscType, OppoSensorType.InputSource, OppoSensorType.HDMIResolution, OppoSensorType.AudioType, OppoSensorType.SubTitleType
    }).ToFrozenSet();

    public static IReadOnlySet<OppoSensorType> GetOppoSensorTypes(in OppoModel oppoModel) =>
        oppoModel is OppoModel.UDP203 or OppoModel.UDP205
            ? AllSensorTypes
            : Non20XSensorTypes;
}