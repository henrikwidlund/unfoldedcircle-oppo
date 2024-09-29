namespace UnfoldedCircle.Models.Sync;

/// <summary>
/// Optional filters
/// </summary>
public record AvailableEntityFilter
{
    [JsonPropertyName("filter")]
    public AvailableEntityFilterInner? Filter { get; init; }
}