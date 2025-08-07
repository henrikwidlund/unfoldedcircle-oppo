using UnfoldedCircle.Models.Shared;

namespace UnfoldedCircle.Models.Sync;

public record MediaPlayerEntityCommandParams
{
    [JsonPropertyName("volume")]
    public ushort? Volume { get; init; }
    
    [JsonPropertyName("media_position")]
    public uint? MediaPosition { get; init; }
    
    [JsonPropertyName("repeat")]
    public RepeatMode? Repeat { get; init; }
    
    [JsonPropertyName("shuffle")]
    public bool? Shuffle { get; init; }
    
    [JsonPropertyName("mode")]
    public string? Mode { get; init; }
    
    [JsonPropertyName("source")]
    public string? Source { get; init; }
}