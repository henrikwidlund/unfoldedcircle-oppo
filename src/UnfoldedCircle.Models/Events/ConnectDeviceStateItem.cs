using UnfoldedCircle.Models.Shared;

namespace UnfoldedCircle.Models.Events;

public record ConnectDeviceStateItem
{
    [JsonPropertyName("state")]
    public required DeviceState State { get; init; }
}