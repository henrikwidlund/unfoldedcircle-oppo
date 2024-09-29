using UnfoldedCircle.Models.Shared;

namespace UnfoldedCircle.Models.Events;

public record DeviceStateItem : DeviceIdObject
{
    [JsonPropertyName("state")]
    public required DeviceState State { get; init; }
}