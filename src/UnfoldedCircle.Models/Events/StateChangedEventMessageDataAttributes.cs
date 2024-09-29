using UnfoldedCircle.Models.Shared;

namespace UnfoldedCircle.Models.Events;

public record StateChangedEventMessageDataAttributes
{
    [JsonPropertyName("state")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public required State State { get; init; }
    
    [JsonPropertyName("media_type")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public MediaType? MediaType { get; init; }
    
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

    [JsonPropertyName("repeat")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public RepeatMode? Repeat { get; init; }
    
    [JsonPropertyName("shuffle")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public bool? Shuffle { get; init; }
}