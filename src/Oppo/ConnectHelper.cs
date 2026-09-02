using System.Net.Sockets;
using System.Threading.RateLimiting;

using Microsoft.Extensions.Logging;

namespace Oppo;

internal static class ConnectHelper
{
    internal static TcpClient CreateTcpClient()
    {
        var tcpClient = new TcpClient { NoDelay = true };
        ConfigureKeepAlive(tcpClient.Client);
        return tcpClient;
    }

    /// <summary>
    /// Enables TCP keepalive so a half-dead connection (e.g. the remote's Wi-Fi radio sleeping without
    /// sending a clean FIN/RST) gets probed and torn down instead of lingering as "connected" until
    /// the next write attempt.
    /// </summary>
    private static void ConfigureKeepAlive(Socket socket)
    {
        try
        {
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
            socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime, 30);
            socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveInterval, 10);
            socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveRetryCount, 3);
        }
        catch (Exception e) when (e is SocketException or PlatformNotSupportedException)
        {
            // Best-effort: not every OS/runtime exposes tunable TCP keepalive. Connection still works without it.
        }
    }

    public static async ValueTask<bool> IsConnectedAsync(
        TcpClient tcpClient,
        string hostName,
        int port,
        SemaphoreSlim semaphore,
        ILogger logger,
        TimeSpan? timeout = null)
    {
        if (tcpClient.Connected)
            return true;

        var acquired = await semaphore.WaitAsync(timeout ?? TimeSpan.FromSeconds(5));
        if (!acquired)
            return tcpClient.Connected;

        try
        {
            return await ConnectIfNeededAsync(tcpClient, hostName, port, logger, timeout);
        }
        finally
        {
            semaphore.Release();
        }
    }

    /// <summary>
    /// Same connect logic as <see cref="IsConnectedAsync"/>, but without acquiring <c>semaphore</c>.
    /// For callers that already hold it for the duration of the call (e.g. SendCommandCore, which owns
    /// the semaphore for the whole command) - acquiring it again here would self-deadlock since
    /// SemaphoreSlim has no concept of a reentrant owner.
    /// </summary>
    internal static ValueTask<bool> IsConnectedNoLockAsync(
        TcpClient tcpClient,
        string hostName,
        int port,
        ILogger logger,
        TimeSpan? timeout = null)
        => ConnectIfNeededAsync(tcpClient, hostName, port, logger, timeout);

    private static async ValueTask<bool> ConnectIfNeededAsync(
        TcpClient tcpClient,
        string hostName,
        int port,
        ILogger logger,
        TimeSpan? timeout)
    {
        if (tcpClient.Connected)
            return true;

        return await DoConnect(true);

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

    internal static TokenBucketRateLimiter CreateRateLimiter() => new(new TokenBucketRateLimiterOptions
    {
        AutoReplenishment = true,
        TokenLimit = 1,
        TokensPerPeriod = 1,
        ReplenishmentPeriod = TimeSpan.FromMilliseconds(100),
        QueueLimit = 30,
        QueueProcessingOrder = QueueProcessingOrder.OldestFirst
    });
}
