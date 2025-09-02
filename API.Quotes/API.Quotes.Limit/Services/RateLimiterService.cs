using System.Threading.RateLimiting;
using System.Threading.Tasks;

namespace API.Quotes.Limit.Services
{
    public class RateLimiterService : IRateLimiterService
    {
        public FixedWindowRateLimiter _globalLimiter { get; }
        public TokenBucketRateLimiter _symbolLimiter { get; }
        public FixedWindowRateLimiter _topLimiter { get; }

        public RateLimiterService() 
        {
            _globalLimiter = new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromSeconds(60),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,//FIFO
                QueueLimit = 0
            });

            _topLimiter = new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromSeconds(60),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,//FIFO
                QueueLimit = 0
            });

            _symbolLimiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
            {
                TokenLimit = 5,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,//FIFO
                QueueLimit = 0,
                ReplenishmentPeriod = TimeSpan.FromSeconds(30),
                TokensPerPeriod = 5,
                AutoReplenishment = true
            });
        }

        public async Task<bool> AcquireLimit(RateLimiter rateLimiter)
        {
            var aquire = await rateLimiter.AcquireAsync(1);

            return aquire.IsAcquired;
        }
    }
}
