using System.ComponentModel.DataAnnotations;

using UnfoldedCircle.Models.Shared;

namespace UnfoldedCircle.Models.Events;

public record MediaPlayerStateChangedEventMessageDataAttributes : StateChangedEventMessageDataAttributes
{
    [JsonPropertyName("state")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public State? State { get; init; }
    
    [JsonPropertyName("volume")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    [Range(0, 100)]
    public ushort? Volume { get; init; }
    
    [JsonPropertyName("muted")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public bool? Muted { get; init; }
    
    [JsonPropertyName("media_position")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public uint? MediaPosition { get; init; }
    
    [JsonPropertyName("media_duration")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public uint? MediaDuration { get; init; }
    
    [JsonPropertyName("media_title")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public string? MediaTitle { get; init; }
    
    [JsonPropertyName("media_artist")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public string? MediaArtist { get; init; }
    
    [JsonPropertyName("media_album")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public string? MediaAlbum { get; init; }
    
    [JsonPropertyName("media_image_url")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public Uri? MediaImageUrl { get; init; }
    
    [JsonPropertyName("media_type")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public MediaType? MediaType { get; init; }

    [JsonPropertyName("repeat")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public RepeatMode? Repeat { get; init; }
    
    [JsonPropertyName("shuffle")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public bool? Shuffle { get; init; }
    
    [JsonPropertyName("source")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public string? Source { get; init; }
    
    [JsonPropertyName("source_list")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string[]? SourceList { get; init; }
    
    [JsonPropertyName("sound_mode")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public string? SoundMode { get; init; }
    
    [JsonPropertyName("sound_mode_list")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string[]? SoundModeList { get; init; }
}