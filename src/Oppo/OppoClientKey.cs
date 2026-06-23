namespace Oppo;

public record struct OppoClientKey(string HostName, in OppoModel Model, in bool UseMediaEvents, in bool UseStreamingEvents,
    string EntityId, string? DeviceId, string? MacAddress);