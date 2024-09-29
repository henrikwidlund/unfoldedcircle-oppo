using Makaretu.Dns;
using UnfoldedCircle.Server.Configuration;

namespace UnfoldedCircle.Server.Dns;

public sealed class MDnsBackgroundService : IHostedService, IDisposable
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ServiceProfile _serviceProfile;
    private ServiceDiscovery? _serviceDiscovery;

    public MDnsBackgroundService(IConfiguration configuration, ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
        // Get the local hostname
        _serviceProfile = new ServiceProfile("oppo-unfolded-circle",
            "_uc-integration._tcp",
            configuration.GetOrDefault<ushort>("UC_INTEGRATION_HTTP_PORT", 9001))
        {
            HostName = $"{System.Net.Dns.GetHostName().Split('.')[0]}.local"
        };

        // Add TXT records
        _serviceProfile.AddProperty("name", "Oppo UDP-20x Blu-ray Player");
        _serviceProfile.AddProperty("ver", "0.0.1");
        _serviceProfile.AddProperty("developer", "Henrik Widlund");
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _serviceDiscovery = await ServiceDiscovery.CreateInstance(loggerFactory: _loggerFactory, cancellationToken: cancellationToken);
        _serviceDiscovery.Advertise(_serviceProfile);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_serviceDiscovery is not null)
            await _serviceDiscovery.Unadvertise(_serviceProfile);
    }

    public void Dispose() => _serviceDiscovery?.Dispose();
}