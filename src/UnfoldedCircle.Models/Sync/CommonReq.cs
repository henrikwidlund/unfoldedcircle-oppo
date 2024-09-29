using System.ComponentModel.DataAnnotations;

namespace UnfoldedCircle.Models.Sync;

/// <summary>
/// Common request message properties.
/// </summary>
public record CommonReq
{
    /// <summary>
    /// Request message identifier.
    /// </summary>
    [JsonPropertyName("kind")]
    public required string Kind { get; init; }

    /// <summary>
    /// Request ID which must be increased for every new request. This ID will be returned in the response message.
    /// </summary>
    [JsonPropertyName("id")]
    public required uint Id { get; init; }

    /// <summary>
    /// One of the defined API request message types.
    /// </summary>
    [JsonPropertyName("msg")]
    [StringLength(32, MinimumLength = 1)]
    public required string Msg { get; init; }
}

public record CommonReq<TMessageData> : CommonReq
{
    [JsonPropertyName("msg_data")]
    public required TMessageData MsgData { get; init; }
}