using System.Globalization;
using System.Net.WebSockets;

namespace UnfoldedCircle.Server.WebSocket;

internal class UnfoldedCircleMiddleware(
    UnfoldedCircleWebSocketHandler unfoldedCircleWebSocketHandler,
    IHostApplicationLifetime applicationLifetime,
    ILoggerFactory loggerFactory,
    ILogger<UnfoldedCircleMiddleware> logger) : IMiddleware
{
    private readonly UnfoldedCircleWebSocketHandler _unfoldedCircleWebSocketHandler = unfoldedCircleWebSocketHandler;
    private readonly IHostApplicationLifetime _applicationLifetime = applicationLifetime;
    private readonly ILoggerFactory _loggerFactory = loggerFactory;
    private readonly ILogger<UnfoldedCircleMiddleware> _logger = logger;

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                var socket = await context.WebSockets.AcceptWebSocketAsync();
                var wsId = $"{context.Connection.RemoteIpAddress?.ToString()}:{context.Connection.RemotePort.ToString(NumberFormatInfo.InvariantInfo)}";

                _logger.LogDebug("[{WSId}] WS: New connection", wsId);

                using var cancellationTokenWrapper = new CancellationTokenWrapper(_applicationLifetime.ApplicationStopping, context.RequestAborted, _loggerFactory.CreateLogger<CancellationTokenWrapper>());
                var result = await _unfoldedCircleWebSocketHandler.HandleWebSocketAsync(socket, wsId, cancellationTokenWrapper);
                await socket.CloseAsync(result.CloseStatus ?? WebSocketCloseStatus.NormalClosure, result.CloseStatusDescription, CancellationToken.None);
                
                _logger.LogDebug("[{WSId}] WS: Connection closed", wsId);
            }
            
            await next(context);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "An error occurred while handling WebSocket connection");
        }
    }
}