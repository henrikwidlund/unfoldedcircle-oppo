namespace UnfoldedCircle.Models.Sync;

public record DriverVersion
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("version")]
    public DriverVersionInner? Version { get; init; }
}