namespace UnfoldedCircle.Models.Events;

/// <summary>
/// A user interface item is either an icon, text or media information from a media-player entity.
/// - Icon and text items can be static or linked to a command specified in the `command` field.
/// - Default size is 1x1 if not specified.
/// </summary>
public record UserInterfaceItem
{
    /// <summary>
    /// Type of the user interface item:
    /// <list type="bullet">
    /// <item><see cref="UserInterfaceItemType.Icon"/>: show an icon, either a UC icon or a custom icon. Field <see cref="Icon"/> must contain the icon identifier.</item>
    /// <item><see cref="UserInterfaceItemType.Text"/>: show text only from field <see cref="Text"/>.</item>
    /// </list>
    /// </summary>
    [JsonPropertyName("type")]
    public required UserInterfaceItemType Type { get; init; }

    /// <summary>
    /// Optional icon identifier. The identifier consists of a prefix and a resource identifier, separated by <c>:</c>.
    /// Available prefixes:
    /// <list type="bullet">
    /// <item><c>uc:</c> - integrated icon font</item>
    /// <item><c>custom:</c> - custom resource</item>
    /// </list>
    /// </summary>
    [JsonPropertyName("icon")]
    public string? Icon { get; init; }

    [JsonPropertyName("text")]
    public string? Text { get; init; }

    [JsonPropertyName("command")]
    public EntityCommand? Command { get; init; }

    [JsonPropertyName("location")]
    public GridLocation? Location { get; init; }

    [JsonPropertyName("size")]
    public GridItemSize? Size { get; init; }
}