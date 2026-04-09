using System.Threading.RateLimiting;

using Microsoft.Extensions.Logging;

namespace Oppo;

internal static class RateLimiterExtensions
{
    extension (FixedWindowRateLimiter limiter)
    {
        public async ValueTask<RateLimitLease> AcquireAsyncWithoutCancellationException(ILogger logger, CancellationToken cancellationToken, string? caller = null)
        {
            try
            {
                var attemptAcquire = limiter.AttemptAcquire();
                if (!attemptAcquire.IsAcquired)
                    return attemptAcquire;

                return await limiter.AcquireAsync(cancellationToken: cancellationToken);
            }
            catch (OperationCanceledException)
            {
                logger.TimeoutWhileAcquiringLease(caller);
                return CancelledLimitLease.Instance;
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
        public override IEnumerable<string> MetadataNames => null!;
    }
}