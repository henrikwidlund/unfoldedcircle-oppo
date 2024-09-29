namespace UnfoldedCircle.Models.Sync;

public record CommonReqOptional<TMessageData> : CommonReq
{
    [JsonPropertyName("msg_data")]
    public TMessageData? MsgData { get; init; }
}