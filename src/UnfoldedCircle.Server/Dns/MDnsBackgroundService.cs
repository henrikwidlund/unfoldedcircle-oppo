using Makaretu.Dns;
using UnfoldedCircle.Server.Configuration;

namespace UnfoldedCircle.Server.Dns;

public sealed class MDnsBackgroundService : IHostedService, IDisposable
{
    private readonly IConfiguration _configuration;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IConfigurationService _configurationService;
    private ServiceProfile? _serviceProfile;
    private ServiceDiscovery? _serviceDiscovery;

    public MDnsBackgroundService(IConfiguration configuration, ILoggerFactory loggerFactory, IConfigurationService configurationService)
    {
        _configuration = configuration;
        _loggerFactory = loggerFactory;
        _configurationService = configurationService;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var driverMetadata = await _configurationService.GetDriverMetadataAsync(cancellationToken);
        // Get the local hostname
        _serviceProfile = new ServiceProfile(driverMetadata.DriverId,
            "_uc-integration._tcp",
            _configuration.GetOrDefault<ushort>("UC_INTEGRATION_HTTP_PORT", 9001))
        {
            HostName = $"{System.Net.Dns.GetHostName().Split('.')[0]}.local"
        };

        // Add TXT records
        _serviceProfile.AddProperty("name", driverMetadata.Name["en"]);
        _serviceProfile.AddProperty("ver", driverMetadata.Version);
        _serviceProfile.AddProperty("developer", driverMetadata.Developer?.Name ?? "N/A");
        _serviceDiscovery = await ServiceDiscovery.CreateInstance(loggerFactory: _loggerFactory, cancellationToken: cancellationToken);
        _serviceDiscovery.Advertise(_serviceProfile);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_serviceProfile is not null && _serviceDiscovery is not null)
            await _serviceDiscovery.Unadvertise(_serviceProfile);
    }

    public void Dispose() => _serviceDiscovery?.Dispose();
}