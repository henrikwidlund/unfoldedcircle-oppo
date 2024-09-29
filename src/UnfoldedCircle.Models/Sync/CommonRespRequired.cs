namespace UnfoldedCircle.Models.Sync;

public record CommonRespRequired<TExtension> : CommonResp
    where TExtension : notnull
{
    /// <summary>
    /// Wrapper for response data object.
    /// </summary>
    [JsonPropertyName("msg_data")]
    public required TExtension MsgData { get; init; }
}