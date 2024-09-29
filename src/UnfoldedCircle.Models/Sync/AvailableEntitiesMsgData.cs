namespace UnfoldedCircle.Models.Sync;

/// <summary>
/// Optional filters
/// </summary>
public record AvailableEntitiesMsgData<TFeature>
    where TFeature : struct, Enum
{
    [JsonPropertyName("filter")]
    public AvailableEntityFilterInner? Filter { get; init; }

    [JsonPropertyName("available_entities")]
    public required AvailableEntity<TFeature>[] AvailableEntities { get; init; }
}