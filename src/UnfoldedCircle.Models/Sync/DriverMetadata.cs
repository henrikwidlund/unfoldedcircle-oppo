using System.ComponentModel.DataAnnotations;

namespace UnfoldedCircle.Models.Sync;

public record DriverMetadata
{
    [JsonPropertyName("driver_id")]
    public required string DriverId { get; init; }

    [JsonPropertyName("name")]
    public required Dictionary<string, string> Name { get; init; }

    [JsonPropertyName("driver_url")]
    public Uri? DriverUrl { get; init; }

    /// <summary>
    /// The JSON <c>auth</c> message is used if a token is configured but no authentication method is set.
    /// </summary>
    [JsonPropertyName("auth_method")]
    public IntgAuthMethod? AuthMethod { get; init; }

    /// <summary>
    /// Driver version, <a href="https://semver.org/">SemVer</a> preferred.
    /// </summary>
    [JsonPropertyName("version")]
    [StringLength(20)]
    public required string Version { get; init; }

    /// <summary>
    /// Optional version check: minimum required Core-API version in the remote.
    /// </summary>
    [JsonPropertyName("min_core_api")]
    [StringLength(20)]
    public string? MinCoreApi { get; init; }

    /// <summary>
    /// Optional icon identifier. If specified the icon will be set.
    /// An empty identifier while updating the object removes the existing icon.
    /// </summary>
    [JsonPropertyName("icon")]
    [StringLength(255)]
    public string? Icon { get; init; }

    [JsonPropertyName("description")]
    public required Dictionary<string, string> Description { get; init; }

    /// <summary>
    /// Optional information about the integration developer.
    /// </summary>
    [JsonPropertyName("developer")]
    public DriverDeveloper? Developer { get; init; }

    /// <summary>
    /// Optional home page url for more information.
    /// </summary>
    [JsonPropertyName("home_page")]
    public Uri? HomePage { get; init; }

    /// <summary>
    /// Driver supports multi-device discovery. <b>Not yet supported.</b>
    /// </summary>
    [JsonPropertyName("device_discovery")]
    public bool? DeviceDiscovery { get; init; }

    /// <summary>
    /// Settings definition page, e.g. to configure an integration driver.
    /// </summary>
    [JsonPropertyName("setup_data_schema")]
    public SettingsPage? SetupDataSchema { get; init; }

    /// <summary>
    /// Release date of the driver.
    /// </summary>
    [JsonPropertyName("release_date")]
    public DateOnly? ReleaseDate { get; init; }
}