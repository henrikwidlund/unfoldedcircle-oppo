namespace UnfoldedCircle.Models.Events;

public record RemoteStateChangedEventMessageDataAttributes : StateChangedEventMessageDataAttributes
{
    [JsonPropertyName("state")]
    public required RemoteState State { get; init; }
}