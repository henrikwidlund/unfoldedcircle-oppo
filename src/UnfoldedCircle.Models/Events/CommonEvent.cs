using System.ComponentModel.DataAnnotations;

namespace UnfoldedCircle.Models.Events;

/// <summary>
/// Common event message properties.
/// </summary>
public record CommonEvent
{
    /// <summary>
    /// Event message identifier.
    /// </summary>
    [JsonPropertyName("kind")]
    public required string Kind { get; init; }

    /// <summary>
    /// One of the defined API request message types.
    /// </summary>
    [JsonPropertyName("msg")]
    [StringLength(32, MinimumLength = 1)]
    public required string Msg { get; init; }
    
    /// <summary>
    /// Event category.
    /// </summary>
    [JsonPropertyName("cat")]
    public string? Cat { get; init; }

    /// <summary>
    /// Optional timestamp when the event was generated.
    /// </summary>
    [JsonPropertyName("ts")]
    public DateTime? TimeStamp { get; init; }
}