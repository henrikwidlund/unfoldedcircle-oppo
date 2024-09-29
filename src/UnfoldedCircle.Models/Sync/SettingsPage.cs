namespace UnfoldedCircle.Models.Sync;

/// <summary>
/// Settings definition page, e.g. to configure an integration driver.
/// </summary>
public record SettingsPage
{
    [JsonPropertyName("title")]
    public required Dictionary<string, string> Title { get; init; }

    /// <summary>
    /// One or multiple input field definitions, with optional pre-set values.
    /// </summary>
    [JsonPropertyName("settings")]
    public required Setting[] Settings { get; init; }
}