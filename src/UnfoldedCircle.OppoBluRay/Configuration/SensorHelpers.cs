using System.Collections.Frozen;

using Oppo;

namespace UnfoldedCircle.OppoBluRay.Configuration;

internal static class SensorHelpers
{
    private static readonly FrozenSet<OppoSensorType> AllSensorTypes = OppoSensorType.GetValues().ToFrozenSet();

    private static readonly FrozenSet<OppoSensorType> Non20XSensorTypes = (new[]
    {
        OppoSensorType.DiscType, OppoSensorType.InputSource, OppoSensorType.HDMIResolution, OppoSensorType.AudioType, OppoSensorType.SubtitleType
    }).ToFrozenSet();

    // Only DiscType/HDMIResolution/HDRStatus are backed by data the Magnetar push protocol actually
    // provides (media/@type, four_k+frame_rate, hdr) - InputSource/AudioType/SubtitleType/ThreeDStatus/
    // AspectRatio have no corresponding field in UpdatePlayState.
    private static readonly FrozenSet<OppoSensorType> MagnetarSensorTypes = (new[]
    {
        OppoSensorType.DiscType, OppoSensorType.HDMIResolution, OppoSensorType.HDRStatus
    }).ToFrozenSet();

    public static IReadOnlySet<OppoSensorType> GetOppoSensorTypes(OppoModel oppoModel) =>
        oppoModel switch
        {
            OppoModel.UDP203 or OppoModel.UDP205 => AllSensorTypes,
            OppoModel.Magnetar => MagnetarSensorTypes,
            _ => Non20XSensorTypes
        };
}
