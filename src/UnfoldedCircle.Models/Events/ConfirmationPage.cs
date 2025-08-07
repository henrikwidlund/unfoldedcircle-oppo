namespace UnfoldedCircle.Models.Events;

/// <summary>
/// Confirmation screen
/// </summary>
public record ConfirmationPage
{
    [JsonPropertyName("title")]
    public required Dictionary<string, string> Title { get; init; }

    /// <summary>
    /// Message to display between title and image (if supplied). Supports Markdown formatting.
    /// </summary>
    [JsonPropertyName("message1")]
    public Dictionary<string, string>? Message1 { get; init; }

    /// <summary>
    /// Optional base64-encoded image.
    /// </summary>
    [JsonPropertyName("image")]
    public string? Image { get; init; }

    /// <summary>
    /// Message to display below message1 or image (if supplied). Supports Markdown formatting.
    /// </summary>
    [JsonPropertyName("message2")]
    public Dictionary<string, string>? Message2 { get; init; }
}