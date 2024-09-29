namespace UnfoldedCircle.Models.Events;

/// <summary>
/// Common event message properties.
/// </summary>
public record CommonEventOptional<TMessageData> : CommonEvent
{
    /// <summary>
    /// Wrapper for event data object.
    /// </summary>
    [JsonPropertyName("msg_data")]
    public TMessageData? MsgData { get; init; }
}