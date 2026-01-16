using System.Net.Sockets;

using Microsoft.Extensions.Logging;

namespace Oppo;

internal static class ConnectHelper
{
    public static async ValueTask<bool> IsConnectedAsync(
        TcpClient tcpClient,
        string hostName,
        int port,
        SemaphoreSlim semaphore,
        ILogger logger,
        TimeSpan? timeout = null)
    {
        if (tcpClient.Connected)
            return tcpClient.Connected;

        var acquired = await semaphore.WaitAsync(timeout ?? TimeSpan.FromSeconds(5));
        if (!acquired)
            return tcpClient.Connected;

        try
        {
            if (tcpClient.Connected)
                return tcpClient.Connected;

            return await DoConnect(true);
        }
        finally
        {
            semaphore.Release();
        }

        async ValueTask<bool> DoConnect(bool allowRetry)
        {
            try
            {
                using var cancellationTokenSource = new CancellationTokenSource(timeout ?? TimeSpan.FromSeconds(3));
                await tcpClient.ConnectAsync(hostName, port, cancellationTokenSource.Token);
                return tcpClient.Connected;
            }
            catch (OperationCanceledException)
            {
                // nothing to do here, ignore
            }
            catch (SocketException) when (allowRetry)
            {
                // Network stack might not be ready, wait a bit and try one more time
                logger.RetryingConnectionAfterSocketException(hostName, port);
                await Task.Delay(500);
                return await DoConnect(false);
            }
            catch (Exception e)
            {
                logger.FailedToConnectToOppoPlayer(e, hostName, port);
            }

            return tcpClient.Connected;
        }
    }
}