using System;

namespace CryptoTrading.Infrastructure.Sqs
{
    public class OrderPlacedEvent
    {
        public Guid CorrelationId { get; set; }
        public int UserId { get; set; }
        public int CryptoId { get; set; }
        public string Symbol { get; set; }
        public string Side { get; set; }
        public string OrderType { get; set; }
        public decimal Quantity { get; set; }
        public decimal? Price { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class OrderExecutedEvent
    {
        public Guid CorrelationId { get; set; }
        public int? TradeId { get; set; }
        public int UserId { get; set; }
        public string Symbol { get; set; }
        public string Side { get; set; }
        public decimal ExecutedQuantity { get; set; }
        public decimal ExecutedPrice { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } // FILLED, REJECTED
        public string ErrorMessage { get; set; }
        public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
    }

    public class MarketTickEvent
    {
        public int CryptoId { get; set; }
        public string Symbol { get; set; }
        public string Name { get; set; }
        public decimal CurrentPrice { get; set; }
        public decimal PriceChange24h { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}
