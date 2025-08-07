using UnfoldedCircle.Models.Shared;

namespace UnfoldedCircle.Models.Sync;

public record EntityCommandMsgData<TCommandId, TEntityCommandParams>
{
    [JsonPropertyName("device_id")]
    public string? DeviceId { get; init; }
    
    [JsonPropertyName("entity_type")]
    public required EntityType EntityType { get; init; }
    
    [JsonPropertyName("entity_id")]
    public required string EntityId { get; init; }
    
    [JsonPropertyName("cmd_id")]
    public required TCommandId CommandId { get; init; }

    [JsonPropertyName("params")]
    public TEntityCommandParams? Params { get; init; }
}