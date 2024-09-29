namespace UnfoldedCircle.Models.Sync;

public record DriverVersionInner
{
    [JsonPropertyName("api")]
    public string? Api { get; init; }

    [JsonPropertyName("driver")]
    public string? Driver { get; init; }
}