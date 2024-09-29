using UnfoldedCircle.Models.Shared;

namespace UnfoldedCircle.Models.Sync;

public record AvailableEntityFilterInner
{
    /// <summary>
    /// Optional device instance filter if driver supports multi instances.
    /// </summary>
    [JsonPropertyName("device_id")]
    public string? DeviceId { get; init; }

    /// <summary>
    /// Supported entities, defined as extensible enum: already known entity types are in the enum, but other string values are allowed for forward compatibility.
    /// </summary>
    [JsonPropertyName("entity_type")]
    public EntityType? EntityType { get; init; }
}