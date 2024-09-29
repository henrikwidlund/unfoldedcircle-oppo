namespace UnfoldedCircle.Server.Configuration;

public record UnfoldedCircleConfiguration
{
    public required List<UnfoldedCircleConfigurationItem> Entities { get; init; }
}