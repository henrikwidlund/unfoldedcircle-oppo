using UnfoldedCircle.OppoBluRay.WebSocket;

namespace UnfoldedCircle.OppoBluRay.Logging;

internal static partial class IntegrationLogger
{
    // WebSocket - Oppo handler logging
    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "[{WSId}] WS: No configurations found")]
    public static partial void NoConfigurationsFound(this ILogger logger, string wsId);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "[{WSId}] WS: No configuration found for identifier '{Identifier}' with type {Type}")]
    public static partial void NoConfigurationFoundForIdentifier(this ILogger logger, string wsId, in ReadOnlyMemory<char> identifier, OppoWebSocketHandler.IdentifierType type);

    [LoggerMessage(EventId = 3, Level = LogLevel.Information, Message = "[{WSId}] WS: No configuration found for device ID '{DeviceId}'")]
    public static partial void NoConfigurationFoundForDeviceId(this ILogger logger, string wsId, in ReadOnlyMemory<char> deviceId);

    // WebSocket - Entity command logging
    [LoggerMessage(EventId = 4, Level = LogLevel.Warning, Message = "[{WSId}] WS: Could not find client for entity ID '{EntityId}'")]
    public static partial void CouldNotFindOppoClientForEntityId(this ILogger logger, string wsId, string entityId);

    [LoggerMessage(EventId = 23, Level = LogLevel.Warning, Message = "[{WSId}] WS: Could not find client for entity ID '{EntityId}'")]
    public static partial void CouldNotFindOppoClientForEntityIdMemory(this ILogger logger, string wsId, ReadOnlyMemory<char> entityId);

    // WebSocket - Entity event logging
    [LoggerMessage(EventId = 9, Level = LogLevel.Debug, Message = "{WSId} Trying to get OppoClientHolder.")]
    public static partial void TryingToGetOppoClientHolder(this ILogger logger, string wsId);

    [LoggerMessage(EventId = 12, Level = LogLevel.Debug, Message = "{WSId} Starting events for {DeviceId}")]
    public static partial void StartingEventsForDevice(this ILogger logger, string wsId, string deviceId);

    // WebSocket - Configuration logging
    [LoggerMessage(EventId = 14, Level = LogLevel.Information, Message = "Adding configuration for entity_id '{EntityId}'")]
    public static partial void AddingConfiguration(this ILogger logger, string entityId);

    [LoggerMessage(EventId = 15, Level = LogLevel.Information, Message = "Updating configuration for entity_id '{EntityId}'")]
    public static partial void UpdatingConfiguration(this ILogger logger, string entityId);

    // AlbumCover service logging
    [LoggerMessage(EventId = 16, Level = LogLevel.Debug, Message = "No album cover found for {Artist} - {Album}")]
    public static partial void NoAlbumCoverFound(this ILogger logger, string artist, string? album);

    [LoggerMessage(EventId = 17, Level = LogLevel.Error, Message = "Failed to fetch {Url}: {StatusCode} - {Content}")]
    public static partial void FailedToFetchUrl(this ILogger logger, string url, System.Net.HttpStatusCode statusCode, string content);

    [LoggerMessage(EventId = 18, Level = LogLevel.Error, Message = "Failed to fetch {Url}")]
    public static partial void FailedToFetchUrlException(this ILogger logger, string url, Exception exception);

    [LoggerMessage(EventId = 19, Level = LogLevel.Error, Message = "Failed to fetch album cover for {ReleaseId}")]
    public static partial void FailedToFetchAlbumCover(this ILogger logger, string releaseId, Exception exception);

    [LoggerMessage(EventId = 20, Level = LogLevel.Error, Message = "{WSId} Failure during event for {Key}.")]
    public static partial void FailureDuringEvent(this ILogger logger, string wsId, string key, Exception exception);

    [LoggerMessage(EventId = 21, Level = LogLevel.Error, Message = "{WSId} Failure during restore.")]
    public static partial void FailureDuringRestore(this ILogger logger, string wsId, Exception exception);

    [LoggerMessage(EventId = 22, Level = LogLevel.Error, Message = "Failure setting streaming verbose mode for {EntityId}.")]
    public static partial void FailureSettingVerboseMode(this ILogger logger, string entityId, Exception exception);
}
