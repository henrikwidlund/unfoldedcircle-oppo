namespace UnfoldedCircle.Server.WebSocket;

public sealed class CancellationTokenWrapper(
    ILogger<CancellationTokenWrapper> logger,
    in CancellationToken applicationStopping,
    in CancellationToken requestAborted) : IDisposable
{
    public readonly CancellationToken ApplicationStopping = applicationStopping;
    public readonly CancellationToken RequestAborted = requestAborted;
    private readonly ILogger _logger = logger;
    private CancellationTokenSource? _broadcastCancellationTokenSource;

    public CancellationTokenSource? GetCurrentBroadcastCancellationTokenSource() => _broadcastCancellationTokenSource;

    public void EnsureNonCancelledBroadcastCancellationTokenSource()
    {
        if (_broadcastCancellationTokenSource is { IsCancellationRequested: false })
            return;
            
        _broadcastCancellationTokenSource?.Dispose();
        _broadcastCancellationTokenSource = new CancellationTokenSource();
        _broadcastCancellationTokenSource.Token.Register(static logger => ((ILogger)logger!).LogWarning("Broadcast cancelled"), _logger);
    }

    public void Dispose() => _broadcastCancellationTokenSource?.Dispose();
}