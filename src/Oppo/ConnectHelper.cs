using System.Diagnostics;
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
    private static readonly TimeSpan DefaultConnectTimeout = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan FirstAttemptTimeout = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan NetworkUnreachableRetryDelay = TimeSpan.FromMilliseconds(50);
    private static readonly TimeSpan SocketErrorRetryDelay = TimeSpan.FromMilliseconds(500);

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
        if (connection.TcpClient.Connected)
            return true;

        var startTimestamp = Stopwatch.GetTimestamp();
        var connectTimeout = timeout ?? DefaultConnectTimeout;
        var isFirstAttempt = true;
        var socketErrorRetried = false;
        SocketException? networkUnreachable = null;
        while (connectTimeout - Stopwatch.GetElapsedTime(startTimestamp) is var remaining && remaining > TimeSpan.Zero)
        {
            // A TcpClient cannot connect again after a dropped connection or a timed out connect.
            connection.PrepareTcpClientForConnect();
            var tcpClient = connection.TcpClient;
            try
            {
                // The OS sends a lost SYN again only after 1 s. A new attempt after a short time is faster.
                using var cancellationTokenSource = new CancellationTokenSource(
                    isFirstAttempt && remaining > FirstAttemptTimeout ? FirstAttemptTimeout : remaining);
                isFirstAttempt = false;
                await tcpClient.ConnectAsync(hostName, port, cancellationTokenSource.Token);
                return tcpClient.Connected;
            }
            catch (OperationCanceledException)
            {
                // Attempt timed out, try again if there is time left
            }
            catch (SocketException e) when (e.SocketErrorCode == SocketError.NetworkUnreachable)
            {
                // Right after wake-up the network is not ready for a short time
                networkUnreachable = e;
                await Task.Delay(NetworkUnreachableRetryDelay);
            }
            catch (SocketException) when (!socketErrorRetried)
            {
                // Network stack might not be ready, wait a bit and try one more time
                socketErrorRetried = true;
                logger.RetryingConnectionAfterSocketException(hostName, port);
                await Task.Delay(SocketErrorRetryDelay);
            }
            catch (Exception e)
            {
                logger.FailedToConnectToOppoPlayer(hostName, port, e);
                return connection.TcpClient.Connected;
            }
        }

        if (networkUnreachable is not null)
            logger.FailedToConnectToOppoPlayer(hostName, port, networkUnreachable);

        return connection.TcpClient.Connected;
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
