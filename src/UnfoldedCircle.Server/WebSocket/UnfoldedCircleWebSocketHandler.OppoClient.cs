using Oppo;

using UnfoldedCircle.Models.Shared;
using UnfoldedCircle.Server.Configuration;
using UnfoldedCircle.Server.Oppo;

namespace UnfoldedCircle.Server.WebSocket;

internal sealed partial class UnfoldedCircleWebSocketHandler
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
        if (entity is not null)
            return new OppoClientKey(entity.Host, entity.Model, entity.UseMediaEvents, entity.UseChapterLengthForMovies);

        _logger.LogInformation("[{WSId}] WS: No configuration found for device ID '{DeviceId}'", wsId, deviceId);
        return null;

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

        if (await oppoClient.IsConnectedAsync())
            return new OppoClientHolder(oppoClient, oppoClientKey.Value);

        _oppoClientFactory.TryDisposeClient(oppoClientKey.Value);
        oppoClient = _oppoClientFactory.TryGetOrCreateClient(oppoClientKey.Value);

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
        var host = msgDataSetupData[OppoConstants.IpAddressKey];
        var deviceId = msgDataSetupData.GetValueOrDefault(OppoConstants.DeviceIdKey, host);
        bool? useMediaEvents = msgDataSetupData.TryGetValue(OppoConstants.UseMediaEventsKey, out var useMediaEventsValue)
            ? useMediaEventsValue.Equals(bool.TrueString, StringComparison.OrdinalIgnoreCase)
            : null;

        bool? useChapterLengthForMovies = msgDataSetupData.TryGetValue(OppoConstants.ChapterOrMovieLengthKey, out var chapterOrMovieLengthValue)
                                        ? chapterOrMovieLengthValue.Equals(OppoConstants.ChapterLengthValue, StringComparison.OrdinalIgnoreCase)
                                        : null;
        
        var entity = configuration.Entities.Find(x => string.Equals(x.DeviceId, deviceId, StringComparison.Ordinal));
        if (entity is null)
        {
            _logger.LogInformation("Adding configuration for device ID '{DeviceId}'", deviceId);
            entity = new UnfoldedCircleConfigurationItem
            {
                Host = host,
                Model = GetOppoModel(msgDataSetupData),
                DeviceId = deviceId,
                DeviceName = OppoConstants.DeviceName,
                EntityId = OppoConstants.EntityId,
                UseMediaEvents = useMediaEvents ?? false,
                UseChapterLengthForMovies = useChapterLengthForMovies ?? false
            };
        }
        else
        {
            _logger.LogInformation("Updating configuration for device ID '{DeviceId}'", deviceId);
            configuration.Entities.Remove(entity);
            entity = entity with
            {
                Host = host,
                UseChapterLengthForMovies = useChapterLengthForMovies ?? entity.UseChapterLengthForMovies,
                UseMediaEvents = useMediaEvents ?? entity.UseMediaEvents
            };
        }
        
        configuration.Entities.Add(entity);
        
        await _configurationService.UpdateConfigurationAsync(configuration, cancellationToken);

        return entity;

        static OppoModel GetOppoModel(Dictionary<string, string> msgDataSetupData)
        {
            if (msgDataSetupData.TryGetValue(OppoConstants.OppoModelKey, out var oppoModel))
            {
                return oppoModel switch
                {
                    _ when oppoModel.Equals(nameof(OppoModel.BDP8395), StringComparison.OrdinalIgnoreCase) => OppoModel.BDP8395,
                    _ when oppoModel.Equals(nameof(OppoModel.BDP10X), StringComparison.OrdinalIgnoreCase) => OppoModel.BDP10X,
                    _ when oppoModel.Equals(nameof(OppoModel.UDP203), StringComparison.OrdinalIgnoreCase) => OppoModel.UDP203,
                    _ => OppoModel.UDP205
                };
            }

            return OppoModel.UDP203;
        }
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
    
    private async ValueTask<DeviceState> GetDeviceState(OppoClientHolder? oppoClientHolder)
    {
        if (oppoClientHolder is null)
            return DeviceState.Disconnected;
        
        try
        {
            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(9));
            while (!cancellationTokenSource.IsCancellationRequested)
            {
                if (await oppoClientHolder.Client.IsConnectedAsync())
                    return DeviceState.Connected;
            }

            return DeviceState.Disconnected;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to get device state");
            return DeviceState.Error;
        }
    }
    
    private record struct RemoveInstruction(string? DeviceId, string[]? EntityIds, string? Host);

    private sealed record OppoClientHolder(IOppoClient Client, in OppoClientKey ClientKey);
}