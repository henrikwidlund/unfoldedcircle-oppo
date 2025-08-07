namespace UnfoldedCircle.Models.Events;

public record EntityCommand
{
    /// <summary>
    /// Simple command name or entity command identifier, as returned in the entity command metadata.
    /// </summary>
    [JsonPropertyName("cmd_id")]
    public required string CmdId { get; init; }

    [JsonPropertyName("hold")]
    public uint? Hold { get; init; }

    [JsonPropertyName("repeat")]
    public uint? Repeat { get; init; }

    [JsonPropertyName("sequence")]
    public string[]? Sequence { get; init; }
}