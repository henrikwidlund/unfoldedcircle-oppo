namespace OppoTelnet;

public record struct OppoClientKey(string HostName, in OppoModel Model, in bool UseMediaEvents, in bool UseChapterLengthForMovies);