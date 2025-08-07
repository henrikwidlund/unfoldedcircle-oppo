namespace UnfoldedCircle.Models.Events;

public record UserInterface
{
    [JsonPropertyName("pages")]
    public UserInterfacePage[]? Pages { get; init; }
}