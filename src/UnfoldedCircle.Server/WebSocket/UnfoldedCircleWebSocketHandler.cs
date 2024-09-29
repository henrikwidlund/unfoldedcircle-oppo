using System.Buffers;
using System.Net.WebSockets;
using System.Text;

using OppoTelnet;

using UnfoldedCircle.Server.AlbumCover;
using UnfoldedCircle.Server.Configuration;
using UnfoldedCircle.Server.Event;
using UnfoldedCircle.Server.Json;
using UnfoldedCircle.Server.Response;

namespace UnfoldedCircle.Server.WebSocket;

internal partial class UnfoldedCircleWebSocketHandler(
    UnfoldedCircleJsonSerializerContext unfoldedCircleJsonSerializerContext,
    IOppoClientFactory oppoClientFactory,
    IConfigurationService configurationService,
    IAlbumCoverService albumCoverService,
    ILogger<UnfoldedCircleWebSocketHandler> logger)
{
    private readonly UnfoldedCircleJsonSerializerContext _unfoldedCircleJsonSerializerContext = unfoldedCircleJsonSerializerContext;
    private readonly IOppoClientFactory _oppoClientFactory = oppoClientFactory;
    private readonly IConfigurationService _configurationService = configurationService;
    private readonly IAlbumCoverService _albumCoverService = albumCoverService;
    private readonly ILogger<UnfoldedCircleWebSocketHandler> _logger = logger;

    public async Task<WebSocketReceiveResult> HandleWebSocketAsync(
        System.Net.WebSockets.WebSocket socket,
        string wsId,
        CancellationTokenWrapper cancellationTokenWrapper)
    {
        await SendAuthResponseAsync(socket,
            wsId,
            cancellationTokenWrapper);
        
        var buffer = ArrayPool<byte>.Shared.Rent(1024 * 4);
        WebSocketReceiveResult result;
        
        do
        {
            result = await socket.ReceiveAsync(buffer, CancellationToken.None);
            if (_logger.IsEnabled(LogLevel.Trace))
                _logger.LogTrace("[{WSId}] WS: Received message '{Message}'", wsId, Encoding.UTF8.GetString(buffer, 0, result.Count));

            if (result.Count == 0)
            {
                _logger.LogTrace("[{WSId}] WS: Received message is not JSON.", wsId);
                continue;
            }

            using var jsonDocument = JsonDocument.Parse(buffer.AsMemory(0, result.Count));
            if (!jsonDocument.RootElement.TryGetProperty("msg", out var msg))
            {
                _logger.LogDebug("[{WSId}] WS: Received message does not contain 'msg' property.", wsId);
                continue;
            }
            
            var messageEvent = MessageEventHelpers.GetMessageEvent(msg, out var rawValue);
            if (messageEvent == MessageEvent.Other)
            {
                _logger.LogInformation("[{WSId}] WS: Unknown message '{Message}'", wsId, rawValue);
                continue;
            }
            
            if (!jsonDocument.RootElement.TryGetProperty("kind", out var kind))
            {
                _logger.LogInformation("[{WSId}] WS: Received message does not contain 'kind' property.", wsId);
                continue;
            }
            
            if (kind.ValueEquals("req"u8))
                await HandleRequestMessage(socket, wsId, messageEvent, jsonDocument, cancellationTokenWrapper);
            else if (kind.ValueEquals("event"u8))
                await HandleEventMessage(socket, wsId, messageEvent, jsonDocument, cancellationTokenWrapper);
        } while (!result.CloseStatus.HasValue && !cancellationTokenWrapper.RequestAborted.IsCancellationRequested);

        return result;
    }
    
    private Task SendAuthResponseAsync(
        System.Net.WebSockets.WebSocket socket,
        string wsId,
        CancellationTokenWrapper cancellationTokenWrapper) =>
        SendAsync(socket,
            ResponsePayloadHelpers.CreateAuthResponsePayload(_unfoldedCircleJsonSerializerContext),
            wsId,
            cancellationTokenWrapper.ApplicationStopping);

    private Task SendAsync(
        System.Net.WebSockets.WebSocket socket,
        ArraySegment<byte> buffer,
        string wsId,
        CancellationToken cancellationToken)
    {
        if (_logger.IsEnabled(LogLevel.Trace))
            _logger.LogTrace("[{WSId}] WS: Sending message '{Message}'", wsId, Encoding.UTF8.GetString(buffer.Array!, buffer.Offset, buffer.Count));

        return socket.SendAsync(buffer, WebSocketMessageType.Text, true, cancellationToken);
    }
}