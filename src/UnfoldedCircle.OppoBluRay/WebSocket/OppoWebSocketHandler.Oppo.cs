using Oppo;

using UnfoldedCircle.Models.Shared;
using UnfoldedCircle.OppoBluRay.Configuration;
using UnfoldedCircle.Server.Extensions;

namespace UnfoldedCircle.OppoBluRay.WebSocket;

public partial class OppoWebSocketHandler
{
    private async Task<OppoClientKey?> TryGetOppoClientKey(
        string wsId,
        IdentifierType identifierType,
        string? identifier,
        CancellationToken cancellationToken)
    {
        var configuration = await _configurationService.GetConfigurationAsync(cancellationToken);
        if (configuration.Entities.Count == 0)
        {
            _logger.LogInformation("[{WSId}] WS: No configurations found", wsId);
            return null;
        }

        var localIdentifier = identifier.GetNullableBaseIdentifier();

        var entity = identifierType switch
        {
            IdentifierType.DeviceId => !string.IsNullOrWhiteSpace(localIdentifier)
                ? configuration.Entities.Find(x => string.Equals(x.DeviceId, localIdentifier, StringComparison.Ordinal))
                : configuration.Entities[0],
            IdentifierType.EntityId => !string.IsNullOrWhiteSpace(localIdentifier)
                ? configuration.Entities.Find(x => string.Equals(x.EntityId, localIdentifier, StringComparison.Ordinal))
                : null,
            _ => throw new ArgumentOutOfRangeException(nameof(identifierType), identifierType, null)
        };

        if (entity is not null)
            return new OppoClientKey(entity.Host, entity.Model, entity.UseMediaEvents, entity.UseChapterLengthForMovies,
                entity.EntityId, entity.DeviceId);

        _logger.LogInformation("[{WSId}] WS: No configuration found for identifier '{Identifier}' with type {Type}",
            wsId, localIdentifier, identifierType.ToString());
        return null;
    }

    private static OppoClientKey GetOppoClientKey(OppoConfigurationItem entity)
    {
        return new OppoClientKey(entity.Host, entity.Model, entity.UseMediaEvents, entity.UseChapterLengthForMovies,
            entity.EntityId, entity.DeviceId);
    }

    private async Task<OppoClientKey[]?> TryGetOppoClientKeys(
        string wsId,
        CancellationToken cancellationToken)
    {
        var configuration = await _configurationService.GetConfigurationAsync(cancellationToken);
        if (configuration.Entities.Count == 0)
        {
            _logger.LogInformation("[{WSId}] WS: No configurations found", wsId);
            return null;
        }

        return configuration.Entities
            .Select(static entity => new OppoClientKey(entity.Host, entity.Model, entity.UseMediaEvents, entity.UseChapterLengthForMovies,
                entity.EntityId, entity.DeviceId))
            .ToArray();
    }

    private enum IdentifierType
    {
        DeviceId,
        EntityId
    }

    private async Task<OppoClientHolder?> TryGetOppoClientHolder(
        string wsId,
        string? identifier,
        IdentifierType identifierType,
        CancellationToken cancellationToken)
    {
        var oppoClientKey = await TryGetOppoClientKey(wsId, identifierType, identifier, cancellationToken);
        if (oppoClientKey is null)
            return null;

        var oppoClient = await _oppoClientFactory.TryGetOrCreateClient(oppoClientKey.Value, cancellationToken);
        if (oppoClient is null)
            return null;

        if (await oppoClient.IsConnectedAsync())
            return new OppoClientHolder(oppoClient, oppoClientKey.Value);

        _oppoClientFactory.TryDisposeClient(oppoClientKey.Value);
        oppoClient = await _oppoClientFactory.TryGetOrCreateClient(oppoClientKey.Value, cancellationToken);

        return oppoClient is null ? null : new OppoClientHolder(oppoClient, oppoClientKey.Value);
    }

    private async Task<OppoClientHolder?> TryGetOppoClientHolder(
        OppoConfigurationItem entity,
        CancellationToken cancellationToken)
    {
        var oppoClientKey = GetOppoClientKey(entity);

        var oppoClient = await _oppoClientFactory.TryGetOrCreateClient(oppoClientKey, cancellationToken);
        if (oppoClient is null)
            return null;

        if (await oppoClient.IsConnectedAsync())
            return new OppoClientHolder(oppoClient, oppoClientKey);

        _oppoClientFactory.TryDisposeClient(oppoClientKey);
        oppoClient = await _oppoClientFactory.TryGetOrCreateClient(oppoClientKey, cancellationToken);

        return oppoClient is null ? null : new OppoClientHolder(oppoClient, oppoClientKey);
    }

    private async Task<List<OppoClientHolder>?> TryGetOppoClientHolders(
        string wsId,
        CancellationToken cancellationToken)
    {
        var oppoClientKeys = await TryGetOppoClientKeys(wsId, cancellationToken);
        if (oppoClientKeys is not { Length: > 0 })
            return null;

        var oppoClients = new List<OppoClientHolder>(oppoClientKeys.Length);
        foreach (var oppoClientKey in oppoClientKeys)
        {
            var oppoClient = await _oppoClientFactory.TryGetOrCreateClient(oppoClientKey, cancellationToken);
            if (oppoClient is null)
                return null;

            if (await oppoClient.IsConnectedAsync())
            {
                oppoClients.Add(new OppoClientHolder(oppoClient, oppoClientKey));
                continue;
            }

            _oppoClientFactory.TryDisposeClient(oppoClientKey);
            oppoClient = await _oppoClientFactory.TryGetOrCreateClient(oppoClientKey, cancellationToken);

            if (oppoClient is null)
                continue;
            oppoClients.Add(new OppoClientHolder(oppoClient, oppoClientKey));
        }

        return oppoClients;
    }

    private async Task<List<OppoConfigurationItem>?> GetEntities(
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

        if (!string.IsNullOrEmpty(deviceId))
        {
            var localDeviceId = deviceId.GetBaseIdentifier();
            var entity = configuration.Entities.Find(x => string.Equals(x.DeviceId, localDeviceId, StringComparison.Ordinal));
            if (entity is not null)
                return [entity];

            _logger.LogInformation("[{WSId}] WS: No configuration found for device ID '{DeviceId}'", wsId, localDeviceId);
            return null;
        }

        return configuration.Entities;
    }

    private async ValueTask<bool> TryDisconnectOppoClients(
        string wsId,
        string? deviceId,
        CancellationToken cancellationToken)
    {
        var oppoClientKeys = await TryGetOppoClientKeys(wsId, cancellationToken);
        if (oppoClientKeys is not { Length: > 0 })
            return false;

        foreach (var oppoClientKey in oppoClientKeys)
        {
            if (!string.IsNullOrEmpty(deviceId) && !string.Equals(oppoClientKey.DeviceId, deviceId, StringComparison.Ordinal))
                continue;

            _oppoClientFactory.TryDisposeClient(oppoClientKey);
        }

        return true;
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

    private sealed record OppoClientHolder(IOppoClient Client, in OppoClientKey ClientKey);
}