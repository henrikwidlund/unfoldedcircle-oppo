namespace OppoTelnet;

public record struct OppoClientKey(string HostName, in int Port, in bool UseMediaEvents, in bool UseChapterLengthForMovies);