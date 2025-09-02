namespace API.Quotes.Limit.Models
{
    public class StockQuote
    {
        public string Symbol { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public DateTime Timestamp { get; set; }
        public long MarketCap { get; set; }
    }
}
