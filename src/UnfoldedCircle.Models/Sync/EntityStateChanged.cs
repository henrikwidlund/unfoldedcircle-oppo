using UnfoldedCircle.Models.Shared;

namespace UnfoldedCircle.Models.Sync;

/// <summary>
/// See entity documentation for <see cref="Attributes"/> payload.
/// </summary>
public record EntityStateChanged<TAttribute>
    where TAttribute : struct, Enum
{
    [JsonPropertyName("device_id")]
    public string? DeviceId { get; init; }
    
    [JsonPropertyName("entity_type")]
    public required EntityType EntityType { get; init; }
    
    [JsonPropertyName("entity_id")]
    public required string EntityId { get; init; }
    
    [JsonPropertyName("attributes")]
    public required TAttribute[] Attributes { get; init; }
}