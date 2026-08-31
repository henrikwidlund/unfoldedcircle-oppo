namespace Oppo;

public record struct OppoClientKey(string HostName, OppoModel Model, bool UseMediaEvents, bool UseStreamingEvents,
    string EntityId, string? DeviceId, string? MacAddress);
