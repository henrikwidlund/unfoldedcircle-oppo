using UnfoldedCircle.Server.Json;

namespace UnfoldedCircle.Server.Configuration;

internal class ConfigurationService(IConfiguration configuration, UnfoldedCircleJsonSerializerContext jsonSerializerContext)
    : IConfigurationService
{
    private readonly IConfiguration _configuration = configuration;
    private readonly UnfoldedCircleJsonSerializerContext _jsonSerializerContext = jsonSerializerContext;
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
                    _jsonSerializerContext.UnfoldedCircleConfiguration,
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
                await using var configurationFile = File.OpenWrite(ConfigurationFilePath);
                await JsonSerializer.SerializeAsync(configurationFile,
                    _unfoldedCircleConfiguration,
                    _jsonSerializerContext.UnfoldedCircleConfiguration,
                    cancellationToken);
                
                return _unfoldedCircleConfiguration;
            }
        }
        finally
        {
            _unfoldedCircleConfigSemaphore.Release();
        }
    }
    
    public async Task<UnfoldedCircleConfiguration> UpdateConfigurationAsync(UnfoldedCircleConfiguration configurations, CancellationToken cancellationToken = default)
    {
        await _unfoldedCircleConfigSemaphore.WaitAsync(cancellationToken);
        
        try
        {
            await using var configurationFileStream = File.OpenWrite(ConfigurationFilePath);
            await JsonSerializer.SerializeAsync(configurationFileStream, configurations, _jsonSerializerContext.UnfoldedCircleConfiguration, cancellationToken);
            _unfoldedCircleConfiguration = configurations;
            return _unfoldedCircleConfiguration;
        }
        finally
        {
            _unfoldedCircleConfigSemaphore.Release();
        }
    }
}