namespace UnfoldedCircle.Models.Events;

/// <summary>
/// Item size in the button grid. Default size if not specified: 1 x 1
/// </summary>
public record GridItemSize
{
    [JsonPropertyName("width")]
    public ushort Width { get; init; } = 1;

    [JsonPropertyName("height")]
    public ushort Height { get; init; } = 1;
}