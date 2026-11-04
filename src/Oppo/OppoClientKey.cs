namespace Oppo;

// HostName is whatever the control connection is made to: the player, or the serial bridge in front of it.
public record struct OppoClientKey(string HostName, OppoModel Model, bool UseMediaEvents, bool UseStreamingEvents,
    string EntityId, string? DeviceId, string? MacAddress, ushort? ControlPort = null, bool UseSerialBridge = false);
