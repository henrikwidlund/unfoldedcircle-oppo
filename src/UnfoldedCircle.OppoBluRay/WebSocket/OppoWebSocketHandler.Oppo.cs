using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;

using Oppo;

using UnfoldedCircle.OppoBluRay.Configuration;
using UnfoldedCircle.OppoBluRay.Logging;
using UnfoldedCircle.Server.Extensions;

namespace UnfoldedCircle.OppoBluRay.WebSocket;

public partial class OppoWebSocketHandler
{
    private async Task<OppoClientKey?> TryGetOppoClientKeyAsync(
        string wsId,
        IdentifierType identifierType,
        string? identifier,
        CancellationToken cancellationToken)
    {
        var configuration = await _configurationService.GetConfigurationAsync(cancellationToken);
        if (configuration.Entities.Count == 0)
        {
            _logger.NoConfigurationsFound(wsId);
            return null;
        }

        var localIdentifier = identifier?.AsMemory().GetBaseIdentifier();

        var entity = identifierType switch
        {
            IdentifierType.DeviceId => localIdentifier is { Span.IsEmpty: false }
                ? configuration.Entities.FirstOrDefault(x => x.DeviceId?.AsSpan().Equals(localIdentifier.Value.Span, StringComparison.OrdinalIgnoreCase) is true)
                : configuration.Entities[0],
            IdentifierType.EntityId => localIdentifier is { Span.IsEmpty: false }
                ? configuration.Entities.FirstOrDefault(x => x.EntityId.AsSpan().Equals(localIdentifier.Value.Span, StringComparison.OrdinalIgnoreCase))
                : null,
            _ => throw new ArgumentOutOfRangeException(nameof(identifierType), identifierType, null)
        };

        if (entity is not null)
            return new OppoClientKey(entity.Host, entity.Model, entity.UseMediaEvents,
                entity.UseStreamingEvents,
                entity.EntityId, entity.DeviceId, entity.MacAddress);

        _logger.NoConfigurationFoundForIdentifier(wsId, localIdentifier ?? default, identifierType);
        return null;
    }

    private async Task<OppoClientKey[]?> TryGetOppoClientKeysAsync(
        string wsId,
        CancellationToken cancellationToken)
    {
        var configuration = await _configurationService.GetConfigurationAsync(cancellationToken);
        if (configuration.Entities.Count == 0)
        {
            _logger.NoConfigurationsFound(wsId);
            return null;
        }

        return [.. configuration.Entities
            .Select(static entity => new OppoClientKey(entity.Host, entity.Model, entity.UseMediaEvents,
                entity.UseStreamingEvents,
                entity.EntityId, entity.DeviceId, entity.MacAddress))];
    }

    internal enum IdentifierType : byte
    {
        DeviceId,
        EntityId
    }

    private static readonly TimeSpan MinConnectBackoff = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan MaxConnectBackoff = TimeSpan.FromMinutes(2);
    private readonly ConcurrentDictionary<OppoClientKey, ConnectBackoff> _connectBackoffs = new();

    private bool IsInConnectBackoff(in OppoClientKey oppoClientKey) =>
        _connectBackoffs.TryGetValue(oppoClientKey, out var backoff)
        && Stopwatch.GetElapsedTime(backoff.FailedTimestamp) < backoff.Delay;

    private void RecordConnectResult(in OppoClientKey oppoClientKey, bool connected)
    {
        if (connected)
        {
            _connectBackoffs.TryRemove(oppoClientKey, out _);
            return;
        }

        _connectBackoffs.AddOrUpdate(oppoClientKey,
            static _ => new ConnectBackoff(Stopwatch.GetTimestamp(), MinConnectBackoff),
            static (_, backoff) => new ConnectBackoff(Stopwatch.GetTimestamp(),
                TimeSpan.FromTicks(Math.Min(backoff.Delay.Ticks * 2, MaxConnectBackoff.Ticks))));
    }

    /// <summary>
    /// Connects only if not in back-off. Used by polling for already known clients.
    /// </summary>
    private async ValueTask<bool> IsConnectedWithBackoffAsync(OppoClientHolder oppoClientHolder)
    {
        if (IsInConnectBackoff(oppoClientHolder.ClientKey))
            return false;

        var connected = await oppoClientHolder.Client.IsConnectedAsync();
        RecordConnectResult(oppoClientHolder.ClientKey, connected);
        return connected;
    }

    private async Task<OppoClientHolder?> TryGetOppoClientHolderAsync(
        string wsId,
        string? identifier,
        IdentifierType identifierType,
        bool isBackground,
        CancellationToken cancellationToken)
    {
        var oppoClientKey = await TryGetOppoClientKeyAsync(wsId, identifierType, identifier, cancellationToken);
        return oppoClientKey is null ? null : await TryGetOppoClientHolderAsync(oppoClientKey.Value, isBackground, cancellationToken);
    }

    /// <param name="oppoClientKey"></param>
    /// <param name="isBackground">
    /// True for polling: skipped during back-off, no reconnect with a new client.
    /// False for user actions: clears back-off so polling resumes right away.
    /// </param>
    /// <param name="cancellationToken"></param>
    private async Task<OppoClientHolder?> TryGetOppoClientHolderAsync(
        OppoClientKey oppoClientKey,
        bool isBackground,
        CancellationToken cancellationToken)
    {
        if (!isBackground)
            _connectBackoffs.TryRemove(oppoClientKey, out _);
        else if (IsInConnectBackoff(oppoClientKey))
        {
            _logger.SkippingConnectDuringBackoff(oppoClientKey.EntityId);
            return null;
        }

        var oppoClient = await _oppoClientFactory.TryGetOrCreateClient(oppoClientKey, cancellationToken);
        if (oppoClient is null)
            return null;

        if (await oppoClient.IsConnectedAsync())
        {
            RecordConnectResult(oppoClientKey, true);
            return new OppoClientHolder(oppoClient, oppoClientKey);
        }

        if (isBackground)
        {
            // Return the unconnected client so polling can still report state (e.g. Magnetar as Off).
            RecordConnectResult(oppoClientKey, false);
            return new OppoClientHolder(oppoClient, oppoClientKey);
        }

        _oppoClientFactory.TryDisposeClient(oppoClientKey);
        oppoClient = await _oppoClientFactory.TryGetOrCreateClient(oppoClientKey, cancellationToken);

        return oppoClient is null ? null : new OppoClientHolder(oppoClient, oppoClientKey);
    }

    private async Task<List<OppoClientHolder>?> TryGetOppoClientHolders(
        string wsId,
        CancellationToken cancellationToken)
    {
        var oppoClientKeys = await TryGetOppoClientKeysAsync(wsId, cancellationToken);
        if (oppoClientKeys is not { Length: > 0 })
            return null;

        var oppoClients = new List<OppoClientHolder>(oppoClientKeys.Length);
        foreach (var oppoClientKey in oppoClientKeys)
        {
            var oppoClient = await _oppoClientFactory.TryGetOrCreateClient(oppoClientKey, cancellationToken);
            if (oppoClient is null)
                continue;

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

    private async Task<List<OppoConfigurationItem>?> GetEntitiesAsync(
        string wsId,
        string? deviceId,
        CancellationToken cancellationToken)
    {
        var configuration = await _configurationService.GetConfigurationAsync(cancellationToken);
        if (configuration.Entities.Count == 0)
        {
            _logger.NoConfigurationsFound(wsId);
            return null;
        }

        if (!string.IsNullOrEmpty(deviceId))
        {
            var localDeviceId = deviceId.AsMemory().GetBaseIdentifier();
            var entity = configuration.Entities.FirstOrDefault(x => x.DeviceId?.AsSpan().Equals(localDeviceId.Span, StringComparison.OrdinalIgnoreCase) == true);
            if (entity is not null)
                return [entity];

            _logger.NoConfigurationFoundForDeviceId(wsId, localDeviceId);
            return null;
        }

        return configuration.Entities;
    }

    private async ValueTask<bool> TryDisconnectOppoClientsAsync(
        string wsId,
        string? deviceId,
        CancellationToken cancellationToken)
    {
        var oppoClientKeys = await TryGetOppoClientKeysAsync(wsId, cancellationToken);
        if (oppoClientKeys is not { Length: > 0 })
            return false;

        var localDeviceId = deviceId?.AsMemory().GetBaseIdentifier();
        foreach (var oppoClientKey in oppoClientKeys)
        {
            if (localDeviceId is not null && !localDeviceId.Value.Span.IsEmpty &&
                oppoClientKey.DeviceId is not null && !oppoClientKey.DeviceId.AsSpan().Equals(localDeviceId.Value.Span, StringComparison.OrdinalIgnoreCase))
                continue;

            _oppoClientFactory.TryDisposeClient(oppoClientKey);
        }

        return true;
    }

    private sealed record OppoClientHolder(IOppoClient Client, in OppoClientKey ClientKey);

    [StructLayout(LayoutKind.Auto)]
    private readonly record struct ConnectBackoff(long FailedTimestamp, TimeSpan Delay);
}
