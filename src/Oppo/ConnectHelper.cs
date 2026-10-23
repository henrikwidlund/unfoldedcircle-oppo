using System.Net.Sockets;
using System.Threading.RateLimiting;

using Microsoft.Extensions.Logging;

namespace Oppo;

/// <summary>
/// A client that owns a replaceable <see cref="System.Net.Sockets.TcpClient"/>.
/// </summary>
internal interface ITcpConnection
{
    TcpClient TcpClient { get; }

    /// <summary>
    /// Called before each connect attempt. Replaces <see cref="TcpClient"/> if it was used before.
    /// Caller must hold the client semaphore.
    /// </summary>
    void PrepareTcpClientForConnect();
}

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
        ITcpConnection connection,
        string hostName,
        int port,
        SemaphoreSlim semaphore,
        ILogger logger,
        TimeSpan? timeout = null)
    {
        if (connection.TcpClient.Connected)
            return true;

        var acquired = await semaphore.WaitAsync(timeout ?? TimeSpan.FromSeconds(5));
        if (!acquired)
            return connection.TcpClient.Connected;

        try
        {
            return await ConnectIfNeededAsync(connection, hostName, port, logger, timeout);
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
        ITcpConnection connection,
        string hostName,
        int port,
        ILogger logger,
        TimeSpan? timeout = null)
        => ConnectIfNeededAsync(connection, hostName, port, logger, timeout);

    private static async ValueTask<bool> ConnectIfNeededAsync(
        ITcpConnection connection,
        string hostName,
        int port,
        ILogger logger,
        TimeSpan? timeout)
    {
        return connection.TcpClient.Connected || await DoConnect(true);

        async ValueTask<bool> DoConnect(bool allowRetry)
        {
            // A TcpClient cannot connect again after a dropped connection or a timed out connect.
            connection.PrepareTcpClientForConnect();
            var tcpClient = connection.TcpClient;
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
                logger.FailedToConnectToOppoPlayer(hostName, port, e);
            }

            return connection.TcpClient.Connected;
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
