using UnfoldedCircle.Models.Shared;

namespace UnfoldedCircle.Models.Sync;

public record AvailableEntity<TFeature>
    where TFeature : struct, Enum
{
    /// <summary>
    /// Unique entity identifier.
    /// </summary>
    [JsonPropertyName("entity_id")]
    public required string EntityId { get; init; }

    /// <summary>
    /// Discriminator value for the concrete entity device type.
    /// </summary>
    [JsonPropertyName("entity_type")]
    public required EntityType EntityType { get; init; }

    /// <summary>
    /// Optional associated device, if the integration driver supports multiple devices.
    /// </summary>
    [JsonPropertyName("device_id")]
    public string? DeviceId { get; init; }

    [JsonPropertyName("features")]
    public ISet<TFeature>? Features { get; init; }

    [JsonPropertyName("name")]
    public required Dictionary<string, string> Name { get; init; }

    /// <summary>
    /// Optional area if supported by the integration, e.g. Living room. This information might be used by the UI in the setup process to automatically create profile pages for all areas returned in the available entities.
    /// </summary>
    [JsonPropertyName("area")]
    public string? Area { get; init; }
    
    [JsonPropertyName("options")]
    public Dictionary<string, ISet<string>>? Options { get; init; }
}