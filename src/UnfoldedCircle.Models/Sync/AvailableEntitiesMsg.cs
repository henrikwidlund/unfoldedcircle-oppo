namespace UnfoldedCircle.Models.Sync;

/// <summary>
/// Available entities response.
/// </summary>
public record AvailableEntitiesMsg<TFeature> : CommonRespRequired<AvailableEntitiesMsgData<TFeature>>
    where TFeature : struct, Enum;