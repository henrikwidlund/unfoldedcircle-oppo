using UnfoldedCircle.Models.Sync;
using UnfoldedCircle.Server.Json;

namespace UnfoldedCircle.Server.Configuration;

internal sealed class ConfigurationService(IConfiguration configuration)
    : IConfigurationService
{
    private readonly IConfiguration _configuration = configuration;
    private string? _ucConfigHome;
    private string UcConfigHome => _ucConfigHome ??= _configuration["UC_CONFIG_HOME"] ?? string.Empty;
    private string ConfigurationFilePath => Path.Combine(UcConfigHome, "configured_entities.json");
    private UnfoldedCircleConfiguration? _unfoldedCircleConfiguration;
    private readonly SemaphoreSlim _unfoldedCircleConfigSemaphore = new(1, 1);

    public async Task<UnfoldedCircleConfiguration> GetConfigurationAsync(CancellationToken cancellationToken = default)
    {
        if (_unfoldedCircleConfiguration is not null)
            return _unfoldedCircleConfiguration;

        await _unfoldedCircleConfigSemaphore.WaitAsync(cancellationToken);

        try
        {
            if (_unfoldedCircleConfiguration is not null)
                return _unfoldedCircleConfiguration;

            if (File.Exists(ConfigurationFilePath))
            {
                await using var configurationFile = File.Open(ConfigurationFilePath, FileMode.Open);
                var deserialized = await JsonSerializer.DeserializeAsync(configurationFile,
                    UnfoldedCircleJsonSerializerContext.Instance.UnfoldedCircleConfiguration,
                    cancellationToken);

                _unfoldedCircleConfiguration = deserialized ?? throw new InvalidOperationException("Failed to deserialize configuration");
                return _unfoldedCircleConfiguration;
            }
            else
            {
                _unfoldedCircleConfiguration = new UnfoldedCircleConfiguration
                {
                    Entities = []
                };
                await using var configurationFile = File.Create(ConfigurationFilePath);
                await JsonSerializer.SerializeAsync(configurationFile,
                    _unfoldedCircleConfiguration,
                    UnfoldedCircleJsonSerializerContext.Instance.UnfoldedCircleConfiguration,
                    cancellationToken);
                
                return _unfoldedCircleConfiguration;
            }
        }
        finally
        {
            _unfoldedCircleConfigSemaphore.Release();
        }
    }
    
    public async Task<UnfoldedCircleConfiguration> UpdateConfigurationAsync(UnfoldedCircleConfiguration configuration, CancellationToken cancellationToken = default)
    {
        await _unfoldedCircleConfigSemaphore.WaitAsync(cancellationToken);
        
        try
        {
            await using var configurationFileStream = File.Create(ConfigurationFilePath);
            await JsonSerializer.SerializeAsync(configurationFileStream, configuration, UnfoldedCircleJsonSerializerContext.Instance.UnfoldedCircleConfiguration, cancellationToken);
            _unfoldedCircleConfiguration = configuration;
            return _unfoldedCircleConfiguration;
        }
        finally
        {
            _unfoldedCircleConfigSemaphore.Release();
        }
    }

    private DriverMetadata? _driverMetadata;

    public async ValueTask<DriverMetadata> GetDriverMetadataAsync(CancellationToken cancellationToken)
    {
        if (_driverMetadata is not null)
            return _driverMetadata;

        await using var fileStream = File.OpenRead("driver.json");
        _driverMetadata = await JsonSerializer.DeserializeAsync<DriverMetadata>(fileStream, UnfoldedCircleJsonSerializerContext.Instance.DriverMetadata, cancellationToken);
        return _driverMetadata ?? throw new InvalidOperationException("Failed to deserialize driver metadata");
    }
}