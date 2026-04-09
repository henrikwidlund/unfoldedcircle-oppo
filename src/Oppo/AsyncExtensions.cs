using System.Threading.RateLimiting;

using Microsoft.Extensions.Logging;

namespace Oppo;

internal static class AsyncExtensions
{
    extension (TokenBucketRateLimiter limiter)
    {
        public async ValueTask<RateLimitLease> AcquireAsyncWithoutCancellationException(ILogger logger,
            CancellationToken cancellationToken,
            string? caller = null)
        {
            try
            {
                // Fast path: permit immediately available — no need to await.
                var attemptAcquire = limiter.AttemptAcquire();
                if (attemptAcquire.IsAcquired)
                    return attemptAcquire;

                attemptAcquire.Dispose();

                // No permit right now — queue up and wait for the next window.
                return await limiter.AcquireAsync(cancellationToken: cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                logger.CancellationWhileAwaiting(caller);
                return CancelledLimitLease.Instance;
            }
        }
    }

    extension(SemaphoreSlim semaphore)
    {
        public async Task<bool> WaitAsyncWithoutCancellationException(ILogger logger,
            TimeSpan timeout,
            CancellationToken cancellationToken,
            string? caller = null)
        {
            try
            {
                return await semaphore.WaitAsync(timeout, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                logger.CancellationWhileAwaiting(caller);
                return false;
            }
        }
    }

    private sealed class CancelledLimitLease : RateLimitLease
    {
        internal static readonly CancelledLimitLease Instance = new();
        public override bool TryGetMetadata(string metadataName, out object? metadata)
        {
            metadata = null;
            return false;
        }

        public override bool IsAcquired => false;
        public override IEnumerable<string> MetadataNames => [];
    }
}