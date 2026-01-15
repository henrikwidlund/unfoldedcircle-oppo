using Oppo;

namespace UnfoldedCircle.OppoBluRay.Configuration;

public record OppoConfigurationItem : UnfoldedCircle.Server.Configuration.UnfoldedCircleConfigurationItem
{
    public required OppoModel Model { get; init; }
    public required bool UseMediaEvents { get; init; }
    public required bool UseChapterLengthForMovies { get; init; }
    public required string? MacAddress { get; init; }
}