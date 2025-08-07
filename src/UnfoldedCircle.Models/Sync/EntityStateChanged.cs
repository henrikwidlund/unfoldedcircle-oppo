using UnfoldedCircle.Models.Shared;

namespace UnfoldedCircle.Models.Sync;

[JsonDerivedType(typeof(MediaPlayerEntityStateChanged))]
[JsonDerivedType(typeof(RemoteEntityStateChanged))]
public abstract record EntityStateChanged
{
    [JsonPropertyName("device_id")]
    public string? DeviceId { get; init; }
    
    [JsonPropertyName("entity_type")]
    public required EntityType EntityType { get; init; }
    
    [JsonPropertyName("entity_id")]
    public required string EntityId { get; init; }
}

/// <summary>
/// See entity documentation for <see cref="Attributes"/> payload.
/// </summary>
public abstract record EntityStateChanged<TAttribute> : EntityStateChanged
    where TAttribute : struct, Enum
{
    [JsonPropertyName("attributes")]
    public required TAttribute[] Attributes { get; init; }
}