using System.ComponentModel.DataAnnotations;

namespace UnfoldedCircle.Models.Sync;

/// <summary>
/// An input setting is of a specific type defined in `field.type` which defines how it is presented to the user.
/// </summary>
public record Setting
{
    /// <summary>
    /// Unique identifier of the setting to be returned with the entered value.
    /// </summary>
    [JsonPropertyName("id")]
    [StringLength(50)]
    public required string Id { get; init; }

    [JsonPropertyName("label")]
    public required Dictionary<string, string> Label { get; init; }

    [JsonPropertyName("field")]
    public required SettingTypeField Field { get; init; }
}