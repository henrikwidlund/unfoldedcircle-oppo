namespace UnfoldedCircle.Models.Sync;

/// <summary>
/// Optional filters
/// </summary>
public record AvailableEntitiesMsgData
{
    [JsonPropertyName("filter")]
    public AvailableEntityFilterInner? Filter { get; init; }

    [JsonPropertyName("available_entities")]
    public required AvailableEntity[] AvailableEntities { get; init; }
}