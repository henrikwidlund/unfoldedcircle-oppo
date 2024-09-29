namespace UnfoldedCircle.Models.Shared;

public record DeviceIdObject
{
    [JsonPropertyName("device_id")]
    public string? DeviceId { get; init; }
}