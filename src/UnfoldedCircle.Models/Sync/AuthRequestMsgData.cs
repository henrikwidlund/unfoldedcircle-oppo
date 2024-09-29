namespace UnfoldedCircle.Models.Sync;

public record AuthRequestMsgData
{
    [JsonPropertyName("token")]
    public required string Token { get; init; }
}