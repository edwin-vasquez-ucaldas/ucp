using API.Quotes.Limit.Models;
using API.Quotes.Limit.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;

namespace API.Quotes.Limit.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QuotesController : ControllerBase
    {
        List<StockQuote> stockQuotes = new List<StockQuote>
        {
            new StockQuote { Symbol = "AAPL", CompanyName = "Apple Inc.", Price = 150.25m, Timestamp = DateTime.UtcNow, MarketCap = 2500000000000 },
            new StockQuote { Symbol = "MSFT", CompanyName = "Microsoft Corporation", Price = 280.50m, Timestamp = DateTime.UtcNow, MarketCap = 2200000000000 },
            new StockQuote { Symbol = "GOOGL", CompanyName = "Alphabet Inc.", Price = 2700.75m, Timestamp = DateTime.UtcNow, MarketCap = 1800000000000 },
            new StockQuote { Symbol = "AMZN", CompanyName = "Amazon.com, Inc.", Price = 3300.10m, Timestamp = DateTime.UtcNow, MarketCap = 1700000000000 },
            new StockQuote { Symbol = "TSLA", CompanyName = "Tesla, Inc.", Price = 700.30m, Timestamp = DateTime.UtcNow, MarketCap = 800000000000 },
            new StockQuote { Symbol = "NVDA", CompanyName = "NVIDIA Corporation", Price = 400.75m, Timestamp = DateTime.UtcNow, MarketCap = 900000000000 },
            new StockQuote { Symbol = "FB", CompanyName = "Meta Platforms, Inc.", Price = 350.60m, Timestamp = DateTime.UtcNow, MarketCap = 950000000000 },
            new StockQuote { Symbol = "NFLX", CompanyName = "Netflix, Inc.", Price = 500.20m, Timestamp = DateTime.UtcNow, MarketCap = 250000000000 },
            new StockQuote { Symbol = "BABA", CompanyName = "Alibaba Group Holding Limited", Price = 200.15m, Timestamp = DateTime.UtcNow, MarketCap = 500000000000 },
            new StockQuote { Symbol = "V", CompanyName = "Visa Inc.", Price = 220.40m, Timestamp = DateTime.UtcNow, MarketCap = 480000000000 }
        };

        private readonly ILogger<QuotesController> _logger;
        private readonly IRateLimiterService _rateLimiterService;
        private readonly IDatabase _redisDb;

        public QuotesController(IRateLimiterService rateLimiterService,
            IConnectionMultiplexer redisDb,
            ILogger<QuotesController> logger)
        {
            _rateLimiterService = rateLimiterService;
            _redisDb = redisDb.GetDatabase();
            _logger = logger;
        }

        [HttpGet]
        public IActionResult GetQuotes()
        {
            _logger.LogInformation("Fetching all stock quotes");
            return Ok(stockQuotes);
        }

        [HttpGet("{symbol}")]
        public async Task<IActionResult> GetQuoteBySymbol(string symbol)
        {
            if (!(await _rateLimiterService.AcquireLimit(_rateLimiterService._symbolLimiter)))
            {
                Response.Headers["Retry-After"] = "30";
                return StatusCode(
                    StatusCodes.Status429TooManyRequests,
                    "Symbol rate limit exceeded. Try again later.");
            }

            _logger.LogInformation($"Fetching stock quote for symbol: {symbol}");
            var quote = stockQuotes.FirstOrDefault(x => x.Symbol.Equals(symbol));
            if(quote == null)
            {
                var message = $"Quote for symbol '{symbol}' not found.";
                _logger.LogError(message);
                return NotFound(message);
            }

            return Ok(quote);
        }

        [HttpGet("top/{top}")]
        public async Task<IActionResult> GetTopQuotes(int top)
        {
            if (!(await _rateLimiterService.AcquireLimit(_rateLimiterService._topLimiter)))
            {
                Response.Headers["Retry-After"] = "60";
                return StatusCode(
                    StatusCodes.Status429TooManyRequests,
                    "Top rate limit exceeded. Try again later.");
            }

            _logger.LogInformation($"Fetching top {top} stock quotes by market cap");

            if(top <= 0 || top > stockQuotes.Count)
            {
                return BadRequest();
            }

            var quotesTop = stockQuotes.OrderByDescending(x => x.MarketCap).Take(top).ToList();

            return Ok(quotesTop);
        }

        [HttpGet("/bySymbol/{symbol}")]
        public async Task<IActionResult> GetQuoteBySymbolRedis(string symbol)
        {
            var token = "token_bucket:symbol";
            var tokenLimit = 5;
            var refillInterval = TimeSpan.FromSeconds(30);

            var cacheTokenResult = await _redisDb.StringGetWithExpiryAsync(token);
            var currentTokens = (int?)(cacheTokenResult.Value) ?? tokenLimit;
            var ttl = cacheTokenResult.Expiry;

            if(!ttl.HasValue || ttl.Value.TotalSeconds <= 0)
            {
                currentTokens = tokenLimit;
                await _redisDb.StringSetAsync(token, currentTokens, refillInterval);
            }

            if(currentTokens <= 0)
            {
                Response.Headers["Retry-After"] = ttl?.TotalSeconds.ToString() ?? "30";
                return StatusCode(
                    StatusCodes.Status429TooManyRequests,
                    "Symbol rate limit exceeded. Try again later.");
            }

            await _redisDb.StringSetAsync(token, currentTokens - 1, refillInterval);

            _logger.LogInformation($"Fetching stock quote for symbol: {symbol}");
            var quote = stockQuotes.FirstOrDefault(x => x.Symbol.Equals(symbol));
            if (quote == null)
            {
                var message = $"Quote for symbol '{symbol}' not found.";
                _logger.LogError(message);
                return NotFound(message);
            }

            return Ok(quote);
        }
    }
}
