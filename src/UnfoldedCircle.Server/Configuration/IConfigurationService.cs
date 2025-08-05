using UnfoldedCircle.Models.Sync;

namespace UnfoldedCircle.Server.Configuration;

public interface IConfigurationService
{
    Task<UnfoldedCircleConfiguration> GetConfigurationAsync(CancellationToken cancellationToken = default);
    Task<UnfoldedCircleConfiguration> UpdateConfigurationAsync(UnfoldedCircleConfiguration configuration, CancellationToken cancellationToken = default);
    ValueTask<DriverMetadata> GetDriverMetadataAsync(CancellationToken cancellationToken);
}