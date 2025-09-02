using System.Threading.RateLimiting;

namespace API.Quotes.Limit.Services
{
    public interface IRateLimiterService
    {
        FixedWindowRateLimiter _globalLimiter { get; }
        TokenBucketRateLimiter _symbolLimiter { get; }
        FixedWindowRateLimiter _topLimiter { get; }

        Task<bool> AcquireLimit(RateLimiter rateLimiter);
    }
}