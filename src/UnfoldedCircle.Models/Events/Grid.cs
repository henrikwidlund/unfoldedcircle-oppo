namespace UnfoldedCircle.Models.Events;

/// <summary>
/// Grid layout size.
/// </summary>
public record Grid
{
    [JsonPropertyName("width")]
    public required ushort Width { get; init; }

    [JsonPropertyName("height")]
    public required ushort Height { get; init; }
}