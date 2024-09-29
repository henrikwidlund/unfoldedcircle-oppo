namespace UnfoldedCircle.Models.Sync;

public record UnsubscribeEventsMsgData
{
    /// <summary>
    /// Only required for multi-device integrations.
    /// </summary>
    [JsonPropertyName("device_id")]
    public string? DeviceId { get; init; }
    
    /// <summary>
    /// Unsubscribe from events only for specified entities.
    /// </summary>
    [JsonPropertyName("entity_ids")]
    public string[]? EntityIds { get; init; }
}