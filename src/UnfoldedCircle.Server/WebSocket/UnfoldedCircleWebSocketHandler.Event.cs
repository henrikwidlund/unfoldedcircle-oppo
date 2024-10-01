using UnfoldedCircle.Models.Shared;
using UnfoldedCircle.Server.Event;
using UnfoldedCircle.Server.Response;

namespace UnfoldedCircle.Server.WebSocket;

internal partial class UnfoldedCircleWebSocketHandler
{
    private async Task HandleEventMessage(
        System.Net.WebSockets.WebSocket socket,
        string wsId,
        MessageEvent messageEvent,
        JsonDocument jsonDocument,
        CancellationTokenWrapper cancellationTokenWrapper)
    {
        switch (messageEvent)
        {
            case MessageEvent.Connect:
            {
                cancellationTokenWrapper.EnsureNonCancelledBroadcastCancellationTokenSource();
                var payload = jsonDocument.Deserialize(_unfoldedCircleJsonSerializerContext.ConnectEvent)!;
                var oppoClientHolder = await TryGetOppoClientHolder(wsId, payload.MsgData?.DeviceId, cancellationTokenWrapper.ApplicationStopping);

                var deviceState = await GetDeviceState(oppoClientHolder);
                await SendAsync(socket,
                    ResponsePayloadHelpers.CreateConnectEventResponsePayload(deviceState,
                        _unfoldedCircleJsonSerializerContext),
                    wsId,
                    cancellationTokenWrapper.ApplicationStopping);

                if (deviceState == DeviceState.Connected)
                    _ = HandleMediaUpdates(socket, wsId, oppoClientHolder!, cancellationTokenWrapper);
                
                return;
            }
            case MessageEvent.Disconnect:
            {
                var payload = jsonDocument.Deserialize(_unfoldedCircleJsonSerializerContext.DisconnectEvent)!;
                await (cancellationTokenWrapper.GetCurrentBroadcastCancellationTokenSource()?.CancelAsync() ?? Task.CompletedTask);
                var success = await TryDisconnectOppoClient(wsId, payload.MsgData?.DeviceId, cancellationTokenWrapper.ApplicationStopping);
                SocketIdEntityIpMap.TryRemove(wsId, out _);
                
                await SendAsync(socket,
                    ResponsePayloadHelpers.CreateConnectEventResponsePayload(success ? DeviceState.Disconnected : DeviceState.Error,
                        _unfoldedCircleJsonSerializerContext),
                    wsId,
                    cancellationTokenWrapper.ApplicationStopping);
                
                return;
            }
            case MessageEvent.AbortDriverSetup:
            {
                _ = jsonDocument.Deserialize(_unfoldedCircleJsonSerializerContext.AbortDriverSetupEvent)!;
                await (cancellationTokenWrapper.GetCurrentBroadcastCancellationTokenSource()?.CancelAsync() ?? Task.CompletedTask);
                if (SocketIdEntityIpMap.TryRemove(wsId, out var host))
                {
                    await RemoveConfiguration(new RemoveInstruction(null, null, host), cancellationTokenWrapper.ApplicationStopping);
                    _logger.LogInformation("[{WSId}] WS: Removed configuration for {Host}", wsId, host);
                }
                
                await SendAsync(socket,
                    ResponsePayloadHelpers.CreateCommonResponsePayload(0, _unfoldedCircleJsonSerializerContext),
                    wsId,
                    cancellationTokenWrapper.ApplicationStopping);
                
                return;
            }
            case MessageEvent.EnterStandby:
                {
                    _ = jsonDocument.Deserialize(_unfoldedCircleJsonSerializerContext.EnterStandbyEvent)!;
                    await (cancellationTokenWrapper.GetCurrentBroadcastCancellationTokenSource()?.CancelAsync() ?? Task.CompletedTask);
                    _oppoClientFactory.TryDisposeAllClients();
                    await SendAsync(socket,
                        ResponsePayloadHelpers.CreateConnectEventResponsePayload(DeviceState.Disconnected, _unfoldedCircleJsonSerializerContext),
                        wsId,
                        cancellationTokenWrapper.ApplicationStopping);
                    return;
                }
            case MessageEvent.ExitStandby:
                {
                    _ = jsonDocument.Deserialize(_unfoldedCircleJsonSerializerContext.ExitStandbyEvent)!;
                    cancellationTokenWrapper.EnsureNonCancelledBroadcastCancellationTokenSource();
                    var oppoClientHolder = await TryGetOppoClientHolder(wsId, null, cancellationTokenWrapper.ApplicationStopping);
                    var deviceState = await GetDeviceState(oppoClientHolder);
                    await SendAsync(socket,
                        ResponsePayloadHelpers.CreateConnectEventResponsePayload(deviceState,
                            _unfoldedCircleJsonSerializerContext),
                        wsId,
                        cancellationTokenWrapper.ApplicationStopping);
                    
                    if (oppoClientHolder is not null && await oppoClientHolder.Client.IsConnectedAsync())
                        _ = HandleMediaUpdates(socket, wsId, oppoClientHolder, cancellationTokenWrapper);
                    return;
                }
            default:
                return;
        }
    }
}