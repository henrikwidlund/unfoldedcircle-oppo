namespace UnfoldedCircle.Models.Sync;

public record CommonRespOptional<TExtension> : CommonResp
{
    /// <summary>
    /// Wrapper for response data object.
    /// </summary>
    [JsonPropertyName("msg_data")]
    public TExtension? MsgData { get; init; }
}