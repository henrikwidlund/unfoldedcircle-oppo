namespace UnfoldedCircle.Models.Sync;

public record ValueRegex
{
    /// <summary>
    /// Optional default value.
    /// </summary>
    [JsonPropertyName("value")]
    public string? Value { get; init; }

    /// <summary>
    /// Optional regex validation pattern for the input value.
    /// </summary>
    [JsonPropertyName("regex")]
    public string? RegEx { get; init; }
}