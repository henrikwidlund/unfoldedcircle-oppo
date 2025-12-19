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

    public static IReadOnlySet<OppoSensorType> GetOppoSensorTypes(in OppoModel oppoModel) =>
        oppoModel is OppoModel.UDP203 or OppoModel.UDP205
            ? AllSensorTypes
            : Non20XSensorTypes;
}