using UnfoldedCircle.Models.Shared;

namespace UnfoldedCircle.Models.Sync;

public record EntityCommandMsgData<TCommandId>
    where TCommandId : struct, Enum
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
    public EntityCommandParams? Params { get; init; }
}