namespace UnfoldedCircle.Server.Configuration;

public record UnfoldedCircleConfigurationItem
{
    public required string Host { get; init; }
    public required int Port { get; init; }
    public string? DeviceId { get; init; }
    public required string DeviceName { get; init; }
    public required string EntityId { get; init; }
    public required bool UseMediaEvents { get; init; }
    public required bool UseChapterLengthForMovies { get; init; }
}