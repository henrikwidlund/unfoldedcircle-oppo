namespace UnfoldedCircle.Models.Events;

/// <summary>
/// Common event message properties.
/// </summary>
public record CommonEventRequired<TMessageData> : CommonEvent
{
    /// <summary>
    /// Wrapper for event data object.
    /// </summary>
    [JsonPropertyName("msg_data")]
    public required TMessageData MsgData { get; init; }
}