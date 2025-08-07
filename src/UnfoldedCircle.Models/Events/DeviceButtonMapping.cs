namespace UnfoldedCircle.Models.Events;

public record DeviceButtonMapping
{
    /// <summary>
    /// Physical button identifier
    /// </summary>
    [JsonPropertyName("button")]
    public required string Button { get; init; }

    [JsonPropertyName("short_press")]
    public EntityCommand? ShortPress { get; init; }

    [JsonPropertyName("long_press")]
    public EntityCommand? LongPress { get; init; }
}