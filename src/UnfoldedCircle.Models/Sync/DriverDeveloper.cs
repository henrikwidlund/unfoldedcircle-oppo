using System.ComponentModel.DataAnnotations;

namespace UnfoldedCircle.Models.Sync;

public record DriverDeveloper
{
    /// <summary>
    /// Optional developer information to display in UI / web-configurator.
    /// </summary>
    [JsonPropertyName("name")]
    [StringLength(50)]
    public string? Name { get; init; }

    /// <summary>
    /// Optional developer home page.
    /// </summary>
    [JsonPropertyName("url")]
    public Uri? Url { get; init; }

    /// <summary>
    /// Optional developer contact email.
    /// </summary>
    [JsonPropertyName("email")]
    public required string Email { get; init; }
}