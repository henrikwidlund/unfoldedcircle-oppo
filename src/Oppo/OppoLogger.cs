using Microsoft.Extensions.Logging;

namespace Oppo;

internal static partial class OppoLogger
{
    private static readonly Action<ILogger, OppoClientKey, Exception> _tryGetOrCreateClientException = LoggerMessage.Define<OppoClientKey>(
        LogLevel.Error,
        new EventId(1, nameof(TryGetOrCreateClientException)),
        "Failed to create client {ClientKey}");

    public static void TryGetOrCreateClientException(this ILogger logger, Exception exception, in OppoClientKey clientKey) =>
        _tryGetOrCreateClientException(logger, clientKey, exception);

    [LoggerMessage(EventId = 2, EventName = nameof(CreateClientSemaphoreFailure), Level = LogLevel.Warning,
        Message = "Failed to acquire semaphore for client creation: {ClientKey}")]
    public static partial void CreateClientSemaphoreFailure(this ILogger logger, in OppoClientKey clientKey);

    private static readonly Action<ILogger, OppoClientKey, Exception> _failedToDisposeClient = LoggerMessage.Define<OppoClientKey>(
        LogLevel.Error,
        new EventId(3, nameof(FailedToDisposeClient)),
        "Failed to dispose client {ClientKey}");

    private static readonly Action<ILogger, int, Exception> _failedToDisposeClientInt = LoggerMessage.Define<int>(
        LogLevel.Error,
        new EventId(4, nameof(FailedToDisposeClient)),
        "Failed to dispose client {ClientKey}");

    public static void FailedToDisposeClient(this ILogger logger, Exception exception, in OppoClientKey clientKey) =>
        _failedToDisposeClient(logger, clientKey, exception);

    public static void FailedToDisposeClient(this ILogger logger, Exception exception, in int clientKey) =>
        _failedToDisposeClientInt(logger, clientKey, exception);

    // OppoClient logging methods
    private static readonly Action<ILogger, string, int, Exception> _failedToConnectToOppoPlayer = LoggerMessage.Define<string, int>(
        LogLevel.Error,
        new EventId(5, nameof(FailedToConnectToOppoPlayer)),
        "Failed to connect to Oppo player at {Host}:{Port}");

    public static void FailedToConnectToOppoPlayer(this ILogger logger, Exception exception, string host, in int port) =>
        _failedToConnectToOppoPlayer(logger, host, port, exception);

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

    private static readonly Action<ILogger, Exception> _failedToSendCommandException = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(11, nameof(FailedToSendCommandException)),
        "Failed to send command.");

    public static void FailedToSendCommandException(this ILogger logger, Exception exception) =>
        _failedToSendCommandException(logger, exception);

    [LoggerMessage(EventId = 12, EventName = nameof(CallerMemberFailed), Level = LogLevel.Error,
        Message = "{CallerMemberName} failed. Response was {Response}")]
    public static partial void CallerMemberFailed(this ILogger logger, string? callerMemberName, string response);
}