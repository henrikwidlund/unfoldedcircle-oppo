namespace UnfoldedCircle.Models.Sync;

public record CommonResp
{
    /// <summary>
    /// Response message identifier.
    /// </summary>
    [JsonPropertyName("kind")]
    public required string Kind { get; init; }

    /// <summary>
    /// Request message ID which is reflected in response message.
    /// </summary>
    [JsonPropertyName("req_id")]
    public required uint ReqId { get; init; }

    /// <summary>
    /// One of the defined API response message types.
    /// </summary>
    [JsonPropertyName("msg")]
    public required string Msg { get; init; }

    /// <summary>
    /// Response code of the operation according to HTTP status codes.
    /// </summary>
    [JsonPropertyName("code")]
    public required int Code { get; init; }
}