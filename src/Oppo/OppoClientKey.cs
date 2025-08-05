namespace Oppo;

public record struct OppoClientKey(string HostName, in OppoModel Model, in bool UseMediaEvents, in bool UseChapterLengthForMovies,
    string EntityId, string? DeviceId);