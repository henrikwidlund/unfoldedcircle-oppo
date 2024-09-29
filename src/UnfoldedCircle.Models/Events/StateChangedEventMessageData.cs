using UnfoldedCircle.Models.Shared;

namespace UnfoldedCircle.Models.Events;

public record StateChangedEventMessageData
{
    [JsonPropertyName("entity_type")]
    public required EntityType EntityType { get; init; }
    
    [JsonPropertyName("entity_id")]
    public required string EntityId { get; init; }
    
    [JsonPropertyName("attributes")]
    public required StateChangedEventMessageDataAttributes Attributes { get; init; }
}