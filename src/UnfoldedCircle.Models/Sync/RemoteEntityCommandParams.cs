namespace UnfoldedCircle.Models.Sync;

public record RemoteEntityCommandParams
{
    [JsonPropertyName("command")]
    public string? Command { get; init; }

    [JsonPropertyName("repeat")]
    public uint? Repeat { get; init; }

    [JsonPropertyName("delay")]
    public uint? Delay { get; init; }

    [JsonPropertyName("sequence")]
    public string[]? Sequence { get; init; }
}