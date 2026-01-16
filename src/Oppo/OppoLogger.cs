using Microsoft.Extensions.Logging;

namespace Oppo;

internal static partial class OppoLogger
{
    private static readonly Action<ILogger, OppoClientKey, Exception> TryGetOrCreateClientExceptionAction = LoggerMessage.Define<OppoClientKey>(
        LogLevel.Error,
        new EventId(1, nameof(TryGetOrCreateClientException)),
        "Failed to create client {ClientKey}");

    public static void TryGetOrCreateClientException(this ILogger logger, Exception exception, in OppoClientKey clientKey) =>
        TryGetOrCreateClientExceptionAction(logger, clientKey, exception);

    [LoggerMessage(EventId = 2, EventName = nameof(CreateClientSemaphoreFailure), Level = LogLevel.Warning,
        Message = "Failed to acquire semaphore for client creation: {ClientKey}")]
    public static partial void CreateClientSemaphoreFailure(this ILogger logger, in OppoClientKey clientKey);

    private static readonly Action<ILogger, OppoClientKey, Exception> FailedToDisposeClientAction = LoggerMessage.Define<OppoClientKey>(
        LogLevel.Error,
        new EventId(3, nameof(FailedToDisposeClient)),
        "Failed to dispose client {ClientKey}");

    private static readonly Action<ILogger, int, Exception> FailedToDisposeClientIntAction = LoggerMessage.Define<int>(
        LogLevel.Error,
        new EventId(4, nameof(FailedToDisposeClient)),
        "Failed to dispose client {ClientKey}");

    // ReSharper disable ConvertToExtensionBlock
    public static void FailedToDisposeClient(this ILogger logger, Exception exception, in OppoClientKey clientKey) =>
        FailedToDisposeClientAction(logger, clientKey, exception);

    public static void FailedToDisposeClient(this ILogger logger, Exception exception, in int clientKey) =>
        FailedToDisposeClientIntAction(logger, clientKey, exception);
    // ReSharper restore ConvertToExtensionBlock

    private static readonly Action<ILogger, string, int, Exception> FailedToConnectToOppoPlayerAction = LoggerMessage.Define<string, int>(
        LogLevel.Error,
        new EventId(5, nameof(FailedToConnectToOppoPlayer)),
        "Failed to connect to player at {Host}:{Port}");

    public static void FailedToConnectToOppoPlayer(this ILogger logger, Exception exception, string host, in int port) =>
        FailedToConnectToOppoPlayerAction(logger, host, port, exception);

    [LoggerMessage(EventId = 6, EventName = nameof(SendingCommand), Level = LogLevel.Trace,
        Message = "Sending command '{Command}'")]
    public static partial void SendingCommand(this ILogger logger, string command);

    [LoggerMessage(EventId = 7, EventName = nameof(TooManyFailedResponses), Level = LogLevel.Warning,
        Message = "{Caller} - Too many failed responses, resetting connection")]
    public static partial void TooManyFailedResponses(this ILogger logger, string? caller);

    [LoggerMessage(EventId = 8, EventName = nameof(ReceivedResponse), Level = LogLevel.Trace,
        Message = "Received response '{Response}'")]
    public static partial void ReceivedResponse(this ILogger logger, string response);

    [LoggerMessage(EventId = 9, EventName = nameof(CommandNotValidAtThisTime), Level = LogLevel.Debug,
        Message = "{Caller} - Command not valid at this time")]
    public static partial void CommandNotValidAtThisTime(this ILogger logger, string? caller);

    [LoggerMessage(EventId = 10, EventName = nameof(FailedToSendCommand), Level = LogLevel.Error,
        Message = "{Caller} - Failed to send command. Response was '{Response}'")]
    public static partial void FailedToSendCommand(this ILogger logger, string? caller, string response);

    private static readonly Action<ILogger, Exception> FailedToSendCommandExceptionAction = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(11, nameof(FailedToSendCommandException)),
        "Failed to send command.");

    public static void FailedToSendCommandException(this ILogger logger, Exception exception) =>
        FailedToSendCommandExceptionAction(logger, exception);

    [LoggerMessage(EventId = 12, EventName = nameof(CallerMemberFailed), Level = LogLevel.Error,
        Message = "{CallerMemberName} failed. Response was {Response}")]
    public static partial void CallerMemberFailed(this ILogger logger, string? callerMemberName, string response);

    [LoggerMessage(EventId = 13, EventName = nameof(RetryingConnectionAfterSocketException), Level = LogLevel.Warning,
        Message = "Retrying connection to player after SocketException (host: {Host}, port: {Port})")]
    public static partial void RetryingConnectionAfterSocketException(this ILogger logger, string host, in int port);

    [LoggerMessage(EventId = 14, EventName = nameof(MissingMacAddress), Level = LogLevel.Error,
        Message = "MAC address is missing for entity ID '{EntityId}'")]
    public static partial void MissingMacAddress(this ILogger logger, string entityId);
}