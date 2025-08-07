namespace UnfoldedCircle.Models.Events;

public record UserInterfacePage
{
    [JsonPropertyName("page_id")]
    public required string PageId { get; init; }

    /// <summary>
    /// Optional page name
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("grid")]
    public required Grid Grid { get; init; }

    [JsonPropertyName("items")]
    public required UserInterfaceItem[] Items { get; init; }
}