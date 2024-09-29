using OppoTelnet;

using UnfoldedCircle.Server.Configuration;

namespace UnfoldedCircle.Server.WebSocket;

internal partial class UnfoldedCircleWebSocketHandler
{
    private async Task<OppoClientKey?> TryGetOppoClientKey(
        string wsId,
        string? deviceId,
        CancellationToken cancellationToken)
    {
        var configuration = await _configurationService.GetConfigurationAsync(cancellationToken);
        if (configuration.Entities.Count == 0)
        {
            _logger.LogInformation("[{WSId}] WS: No configurations found", wsId);
            return null;
        }

        var entity = !string.IsNullOrWhiteSpace(deviceId)
            ? configuration.Entities.Find(x => string.Equals(x.DeviceId, deviceId, StringComparison.Ordinal))
            : configuration.Entities[0];
        if (entity is null)
        {
            _logger.LogInformation("[{WSId}] WS: No configuration found for device ID '{DeviceId}'", wsId, deviceId);
            return null;
        }
        
        return new OppoClientKey(entity.Host, entity.Port, entity.UseMediaEvents, entity.UseChapterLengthForMovies);
    }
    
    private async Task<OppoClientHolder?> TryGetOppoClientHolder(
        string wsId,
        string? deviceId,
        CancellationToken cancellationToken)
    {
        var oppoClientKey = await TryGetOppoClientKey(wsId, deviceId, cancellationToken);
        if (oppoClientKey is null)
            return null;
        
        var oppoClient = _oppoClientFactory.TryGetOrCreateClient(oppoClientKey.Value);
        if (oppoClient is null)
            return null;
        
        if (!oppoClient.IsConnected)
        {
            _oppoClientFactory.TryDisposeClient(oppoClientKey.Value);
            oppoClient = _oppoClientFactory.TryGetOrCreateClient(oppoClientKey.Value);
        }
        
        return oppoClient is null ? null : new OppoClientHolder(oppoClient, oppoClientKey.Value);
    }
    
    private async Task<bool> TryDisconnectOppoClient(
        string wsId,
        string? deviceId,
        CancellationToken cancellationToken)
    {
        var oppoClientKey = await TryGetOppoClientKey(wsId, deviceId, cancellationToken);
        if (oppoClientKey is null)
            return false;
        
        _oppoClientFactory.TryDisposeClient(oppoClientKey.Value);
        return true;
    }
    
    private async Task<UnfoldedCircleConfigurationItem> UpdateConfiguration(
        Dictionary<string, string> msgDataSetupData,
        CancellationToken cancellationToken)
    {
        var configuration = await _configurationService.GetConfigurationAsync(cancellationToken);
        var host = msgDataSetupData["ip_address"];
        var deviceId = msgDataSetupData.GetValueOrDefault("device_id", host);
        bool? useMediaEvents = msgDataSetupData.TryGetValue("use_media_events", out var useMediaEventsValue)
            ? useMediaEventsValue.Equals(bool.TrueString, StringComparison.OrdinalIgnoreCase)
            : null;

        bool? useChapterLengthForMovies = msgDataSetupData.TryGetValue("chapter_or_movie_length", out var chapterOrMovieLengthValue)
                                        ? chapterOrMovieLengthValue.Equals("chapter_length", StringComparison.OrdinalIgnoreCase)
                                        : null;
        
        var entity = configuration.Entities.Find(x => string.Equals(x.DeviceId, deviceId, StringComparison.Ordinal));
        if (entity is null)
        {
            _logger.LogInformation("Adding configuration for device ID '{DeviceId}'", deviceId);
            entity = new UnfoldedCircleConfigurationItem
            {
                Host = host,
                Port = 23,
                DeviceId = deviceId,
                DeviceName = "Oppo UDP-20x Blu-ray Player",
                EntityId = "0393caf1-c9d2-422e-88b5-cb716756334a",
                UseMediaEvents = useMediaEvents ?? false,
                UseChapterLengthForMovies = useChapterLengthForMovies ?? false
            };
            
            configuration.Entities.Add(entity);
        }
        else
        {
            _logger.LogInformation("Updating configuration for device ID '{DeviceId}'", deviceId);
            entity = entity with
            {
                Host = host,
                UseChapterLengthForMovies = useChapterLengthForMovies ?? entity.UseChapterLengthForMovies,
                UseMediaEvents = useMediaEvents ?? entity.UseMediaEvents
            };
        }
        
        await _configurationService.UpdateConfigurationAsync(configuration, cancellationToken);

        return entity;
    }
    
    private async Task RemoveConfiguration(
        RemoveInstruction removeInstruction,
        CancellationToken cancellationToken)
    {
        var configuration = await _configurationService.GetConfigurationAsync(cancellationToken);

        var entities = configuration.Entities.Where(x => string.Equals(x.DeviceId, removeInstruction.DeviceId, StringComparison.Ordinal)
                                                         || removeInstruction.EntityIds?.Contains(x.EntityId, StringComparer.OrdinalIgnoreCase) is true
                                                         || x.Host.Equals(removeInstruction.Host, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        
        foreach (var entity in entities)
        {
            _logger.LogInformation("Removing entity {@Entity}", entity);
            configuration.Entities.Remove(entity);
        }
        
        await _configurationService.UpdateConfigurationAsync(configuration, cancellationToken);
    }
    
    private record struct RemoveInstruction(string? DeviceId, string[]? EntityIds, string? Host);
    
    private record OppoClientHolder(IOppoClient Client, OppoClientKey ClientKey);
}