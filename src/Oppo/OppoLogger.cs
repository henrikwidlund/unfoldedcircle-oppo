using Microsoft.Extensions.Logging;

namespace Oppo;

internal static partial class OppoLogger
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Failed to create client {ClientKey}")]
    public static partial void TryGetOrCreateClientException(this ILogger logger, in OppoClientKey clientKey, Exception exception);

    [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "Failed to acquire semaphore for client creation: {ClientKey}")]
    public static partial void CreateClientSemaphoreFailure(this ILogger logger, in OppoClientKey clientKey);

    [LoggerMessage(EventId = 3, Level = LogLevel.Error, Message = "Failed to dispose client {ClientKey}")]
    public static partial void FailedToDisposeClient(this ILogger logger, in OppoClientKey clientKey, Exception exception);

    [LoggerMessage(EventId = 5, Level = LogLevel.Error, Message = "Failed to connect to player at {Host}:{Port}")]
    public static partial void FailedToConnectToOppoPlayer(this ILogger logger, string host, int port, Exception exception);

    [LoggerMessage(EventId = 6, Level = LogLevel.Trace, Message = "Sending command '{Command}'")]
    public static partial void SendingCommand(this ILogger logger, string command);

    [LoggerMessage(EventId = 7, Level = LogLevel.Warning, Message = "{Caller} - Too many failed responses, resetting connection")]
    public static partial void TooManyFailedResponses(this ILogger logger, string? caller);

    [LoggerMessage(EventId = 8, Level = LogLevel.Trace, Message = "Received response '{Response}'")]
    public static partial void ReceivedResponse(this ILogger logger, string response);

    [LoggerMessage(EventId = 9, Level = LogLevel.Debug, Message = "{Caller} - Command not valid at this time")]
    public static partial void CommandNotValidAtThisTime(this ILogger logger, string? caller);

    [LoggerMessage(EventId = 10, Level = LogLevel.Error, Message = "{Caller} - Failed to send command. Response was '{Response}'")]
    public static partial void FailedToSendCommand(this ILogger logger, string? caller, string response);

    [LoggerMessage(EventId = 11, Level = LogLevel.Error, Message = "Failed to send command.")]
    public static partial void FailedToSendCommandException(this ILogger logger, Exception exception);

    [LoggerMessage(EventId = 12, Level = LogLevel.Error, Message = "{CallerMemberName} failed. Response was {Response}")]
    public static partial void CallerMemberFailed(this ILogger logger, string? callerMemberName, string response);

    [LoggerMessage(EventId = 13, Level = LogLevel.Warning, Message = "Retrying connection to player after SocketException (host: {Host}, port: {Port})")]
    public static partial void RetryingConnectionAfterSocketException(this ILogger logger, string host, int port);

    [LoggerMessage(EventId = 14, Level = LogLevel.Error, Message = "MAC address is missing for entity ID '{EntityId}'")]
    public static partial void MissingMacAddress(this ILogger logger, string entityId);

    [LoggerMessage(EventId = 15, Level = LogLevel.Trace, Message = "Received wire frame '{Frame}'")]
    public static partial void ReceivedWireFrameRaw(this ILogger logger, string? frame);

    [LoggerMessage(EventId = 16, Level = LogLevel.Trace, Message = "Dropped unsolicited streaming frame because there is no active subscriber: '{Frame}'")]
    public static partial void DroppedUnsubscribedStreamingFrame(this ILogger logger, string frame);

    [LoggerMessage(EventId = 17, Level = LogLevel.Trace, Message = "Dropped unsolicited streaming frame because the subscriber channel rejected the write while still active: '{Frame}'")]
    public static partial void DroppedStreamingFrameBecauseChannelRejectedWrite(this ILogger logger, string frame);

    [LoggerMessage(EventId = 18, Level = LogLevel.Trace, Message = "Dropped unsolicited streaming frame because the subscriber channel was completed or replaced: '{Frame}'")]
    public static partial void DroppedStreamingFrameBecauseChannelCompleted(this ILogger logger, string frame);

    [LoggerMessage(EventId = 19, Level = LogLevel.Trace, Message = "Replacing existing streaming subscriber")]
    public static partial void ReplacingStreamingSubscriber(this ILogger logger);

    [LoggerMessage(EventId = 20, Level = LogLevel.Debug, Message = "Received unknown streaming event. Frame '{Frame}'")]
    public static partial void UnknownStreamingStatusCode(this ILogger logger, string frame);

    [LoggerMessage(EventId = 21, Level = LogLevel.Warning, Message = "Reader loop completed because the remote endpoint closed the stream while a command response was pending.")]
    public static partial void ReaderLoopCompletedWithPendingCommand(this ILogger logger);

    [LoggerMessage(EventId = 22, Level = LogLevel.Debug, Message = "Reader loop canceled while a command response was pending.")]
    public static partial void ReaderLoopCanceledWithPendingCommand(this ILogger logger);

    [LoggerMessage(EventId = 23, Level = LogLevel.Warning, Message = "Reader loop failed while a command response was pending.")]
    public static partial void ReaderLoopFailedWithPendingCommand(this ILogger logger);

    [LoggerMessage(EventId = 24, Level = LogLevel.Debug, Message = "{Caller} - Retrying command after player returned ER OVERTIME")]
    public static partial void RetryingAfterOvertime(this ILogger logger, string? caller);

    [LoggerMessage(EventId = 25, Level = LogLevel.Warning, Message = "{Caller} - Failed to acquire rate limit lease, command aborted.")]
    public static partial void FailedToAcquireRateLimitLease(this ILogger logger, string? caller);

    [LoggerMessage(EventId = 26, Level = LogLevel.Information, Message = "{Caller} - Cancellation requested while awaiting.")]
    public static partial void CancellationWhileAwaiting(this ILogger logger, string? caller);
}
