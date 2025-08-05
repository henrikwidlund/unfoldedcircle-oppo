using UnfoldedCircle.Models.Shared;
using UnfoldedCircle.Server.Event;
using UnfoldedCircle.Server.Json;
using UnfoldedCircle.Server.Response;

namespace UnfoldedCircle.Server.WebSocket;

internal sealed partial class UnfoldedCircleWebSocketHandler
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
                var payload = jsonDocument.Deserialize(UnfoldedCircleJsonSerializerContext.Instance.ConnectEvent)!;
                var oppoClientHolders = await TryGetOppoClientHolders(wsId, cancellationTokenWrapper.ApplicationStopping);
                if (oppoClientHolders is { Count: > 0 })
                {
                    foreach (var oppoClientHolder in oppoClientHolders)
                    {
                        var deviceState = await GetDeviceState(oppoClientHolder);
                        await SendAsync(socket,
                            ResponsePayloadHelpers.CreateConnectEventResponsePayload(deviceState),
                            wsId,
                            cancellationTokenWrapper.ApplicationStopping);

                        if (deviceState is DeviceState.Connected)
                            _ = HandleEventUpdates(socket, wsId, oppoClientHolder, cancellationTokenWrapper);
                    }
                }
                
                return;
            }
            case MessageEvent.Disconnect:
            {
                var payload = jsonDocument.Deserialize(UnfoldedCircleJsonSerializerContext.Instance.DisconnectEvent)!;
                await (cancellationTokenWrapper.GetCurrentBroadcastCancellationTokenSource()?.CancelAsync() ?? Task.CompletedTask);
                var success = await TryDisconnectOppoClient(wsId, payload.MsgData?.DeviceId, cancellationTokenWrapper.ApplicationStopping);
                SocketIdEntityIpMap.TryRemove(wsId, out _);
                
                await SendAsync(socket,
                    ResponsePayloadHelpers.CreateConnectEventResponsePayload(success ? DeviceState.Disconnected : DeviceState.Error),
                    wsId,
                    cancellationTokenWrapper.ApplicationStopping);
                
                return;
            }
            case MessageEvent.AbortDriverSetup:
            {
                _ = jsonDocument.Deserialize(UnfoldedCircleJsonSerializerContext.Instance.AbortDriverSetupEvent)!;
                await (cancellationTokenWrapper.GetCurrentBroadcastCancellationTokenSource()?.CancelAsync() ?? Task.CompletedTask);
                if (SocketIdEntityIpMap.TryRemove(wsId, out var host))
                {
                    await RemoveConfiguration(new RemoveInstruction(null, null, host), cancellationTokenWrapper.ApplicationStopping);
                    _logger.LogInformation("[{WSId}] WS: Removed configuration for {Host}", wsId, host);
                }
                
                await SendAsync(socket,
                    ResponsePayloadHelpers.CreateCommonResponsePayload(0),
                    wsId,
                    cancellationTokenWrapper.ApplicationStopping);
                
                return;
            }
            case MessageEvent.EnterStandby:
                {
                    _ = jsonDocument.Deserialize(UnfoldedCircleJsonSerializerContext.Instance.EnterStandbyEvent)!;
                    await (cancellationTokenWrapper.GetCurrentBroadcastCancellationTokenSource()?.CancelAsync() ?? Task.CompletedTask);
                    _oppoClientFactory.TryDisposeAllClients();
                    await SendAsync(socket,
                        ResponsePayloadHelpers.CreateConnectEventResponsePayload(DeviceState.Disconnected),
                        wsId,
                        cancellationTokenWrapper.ApplicationStopping);
                    return;
                }
            case MessageEvent.ExitStandby:
                {
                    _ = jsonDocument.Deserialize(UnfoldedCircleJsonSerializerContext.Instance.ExitStandbyEvent)!;
                    cancellationTokenWrapper.EnsureNonCancelledBroadcastCancellationTokenSource();
                    var oppoClientHolders = await TryGetOppoClientHolders(wsId, cancellationTokenWrapper.ApplicationStopping);
                    if (oppoClientHolders is { Count: > 0 })
                    {
                        foreach (var oppoClientHolder in oppoClientHolders)
                        {
                            var deviceState = await GetDeviceState(oppoClientHolder);
                            await SendAsync(socket,
                                ResponsePayloadHelpers.CreateConnectEventResponsePayload(deviceState),
                                wsId,
                                cancellationTokenWrapper.ApplicationStopping);

                            if (deviceState is DeviceState.Connected)
                                _ = HandleEventUpdates(socket, wsId, oppoClientHolder, cancellationTokenWrapper);
                        }
                    }

                    return;
                }
            default:
                return;
        }
    }
}