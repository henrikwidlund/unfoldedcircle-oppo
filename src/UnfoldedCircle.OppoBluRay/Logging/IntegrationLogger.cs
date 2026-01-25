using Oppo;

using UnfoldedCircle.OppoBluRay.WebSocket;

namespace UnfoldedCircle.OppoBluRay.Logging;

internal static partial class IntegrationLogger
{
    // WebSocket - Oppo handler logging
    [LoggerMessage(EventId = 1, EventName = nameof(NoConfigurationsFound), Level = LogLevel.Information,
        Message = "[{WSId}] WS: No configurations found")]
    public static partial void NoConfigurationsFound(this ILogger logger, string wsId);

    [LoggerMessage(EventId = 2, EventName = nameof(NoConfigurationFoundForIdentifier), Level = LogLevel.Information,
        Message = "[{WSId}] WS: No configuration found for identifier '{Identifier}' with type {Type}")]
    public static partial void NoConfigurationFoundForIdentifier(this ILogger logger, string wsId, in ReadOnlyMemory<char> identifier, in OppoWebSocketHandler.IdentifierType type);

    [LoggerMessage(EventId = 3, EventName = nameof(NoConfigurationFoundForDeviceId), Level = LogLevel.Information,
        Message = "[{WSId}] WS: No configuration found for device ID '{DeviceId}'")]
    public static partial void NoConfigurationFoundForDeviceId(this ILogger logger, string wsId, in ReadOnlyMemory<char> deviceId);

    // WebSocket - Entity command logging
    [LoggerMessage(EventId = 4, EventName = nameof(CouldNotFindOppoClientForEntityId), Level = LogLevel.Warning,
        Message = "[{WSId}] WS: Could not find client for entity ID '{EntityId}'")]
    public static partial void CouldNotFindOppoClientForEntityId(this ILogger logger, string wsId, string entityId);

    [LoggerMessage(EventId = 20, EventName = nameof(CouldNotFindOppoClientForEntityIdMemory), Level = LogLevel.Warning,
        Message = "[{WSId}] WS: Could not find client for entity ID '{EntityId}'")]
    public static partial void CouldNotFindOppoClientForEntityIdMemory(this ILogger logger, string wsId, ReadOnlyMemory<char> entityId);

    // WebSocket - Entity event logging
    [LoggerMessage(EventId = 5, EventName = nameof(SubscribeEventsNotCalled), Level = LogLevel.Debug,
        Message = "{WSId} Subscribe events not called")]
    public static partial void SubscribeEventsNotCalled(this ILogger logger, string wsId);

    [LoggerMessage(EventId = 6, EventName = nameof(BroadcastTokenCancelled), Level = LogLevel.Debug,
        Message = "{WSId} Broadcast token is cancelled {IsCancellationRequested}")]
    public static partial void BroadcastTokenCancelled(this ILogger logger, string wsId, bool? isCancellationRequested);

    [LoggerMessage(EventId = 7, EventName = nameof(EventsAlreadyRunning), Level = LogLevel.Debug,
        Message = "{WSId} Events already running.")]
    public static partial void EventsAlreadyRunning(this ILogger logger, string wsId);

    [LoggerMessage(EventId = 8, EventName = nameof(CouldNotAcquireSemaphore), Level = LogLevel.Error,
        Message = "{WSId} Could not acquire semaphore for broadcasting events for {EntityId}. Will not start broadcasting.")]
    public static partial void CouldNotAcquireSemaphore(this ILogger logger, string wsId, string entityId);

    [LoggerMessage(EventId = 9, EventName = nameof(TryingToGetOppoClientHolder), Level = LogLevel.Debug,
        Message = "{WSId} Trying to get OppoClientHolder.")]
    public static partial void TryingToGetOppoClientHolder(this ILogger logger, string wsId);

    [LoggerMessage(EventId = 10, EventName = nameof(NoLongerSubscribedToEvents), Level = LogLevel.Debug,
        Message = "{WSId} No longer subscribed to events for {EntityId}. Stopping event updates.")]
    public static partial void NoLongerSubscribedToEvents(this ILogger logger, string wsId, string entityId);

    [LoggerMessage(EventId = 11, EventName = nameof(ClientNotConnected), Level = LogLevel.Debug,
        Message = "{WSId} Client not connected. {@ClientKey}")]
    public static partial void ClientNotConnected(this ILogger logger, string wsId, in OppoClientKey clientKey);

    [LoggerMessage(EventId = 12, EventName = nameof(StartingEventsForDevice), Level = LogLevel.Debug,
        Message = "{WSId} Starting events for {DeviceId}")]
    public static partial void StartingEventsForDevice(this ILogger logger, string wsId, string deviceId);

    [LoggerMessage(EventId = 13, EventName = nameof(StoppingMediaUpdates), Level = LogLevel.Debug,
        Message = "{WSId} Stopping media updates for {DeviceId}")]
    public static partial void StoppingMediaUpdates(this ILogger logger, string wsId, string deviceId);

    // WebSocket - Configuration logging
    [LoggerMessage(EventId = 14, EventName = nameof(AddingConfiguration), Level = LogLevel.Information,
        Message = "Adding configuration for entity_id '{EntityId}'")]
    public static partial void AddingConfiguration(this ILogger logger, string entityId);

    [LoggerMessage(EventId = 15, EventName = nameof(UpdatingConfiguration), Level = LogLevel.Information,
        Message = "Updating configuration for entity_id '{EntityId}'")]
    public static partial void UpdatingConfiguration(this ILogger logger, string entityId);

    // AlbumCover service logging
    [LoggerMessage(EventId = 16, EventName = nameof(NoAlbumCoverFound), Level = LogLevel.Debug,
        Message = "No album cover found for {Artist} - {Album}")]
    public static partial void NoAlbumCoverFound(this ILogger logger, string artist, string? album);

    [LoggerMessage(EventId = 17, EventName = nameof(FailedToFetchUrl), Level = LogLevel.Error,
        Message = "Failed to fetch {Url}: {StatusCode} - {Content}")]
    public static partial void FailedToFetchUrl(this ILogger logger, string url, in System.Net.HttpStatusCode statusCode, string content);

    private static readonly Action<ILogger, string, Exception> FailedToFetchUrlExceptionAction = LoggerMessage.Define<string>(
        LogLevel.Error,
        new EventId(18, nameof(FailedToFetchUrlException)),
        "Failed to fetch {Url}");

    public static void FailedToFetchUrlException(this ILogger logger, Exception exception, string url) =>
        FailedToFetchUrlExceptionAction(logger, url, exception);

    private static readonly Action<ILogger, string, Exception> FailedToFetchAlbumCoverAction = LoggerMessage.Define<string>(
        LogLevel.Error,
        new EventId(19, nameof(FailedToFetchAlbumCover)),
        "Failed to fetch album cover for {ReleaseId}");

    public static void FailedToFetchAlbumCover(this ILogger logger, Exception exception, string releaseId) =>
        FailedToFetchAlbumCoverAction(logger, releaseId, exception);

    [LoggerMessage(EventId = 21, EventName = nameof(CouldNotFindOppoClientHolderForEvents), Level = LogLevel.Error,
        Message = "[{WSId}] WS: Could not find OppoClientHolder for events")]
    public static partial void CouldNotFindOppoClientHolderForEvents(this ILogger logger, string wsId);
}

