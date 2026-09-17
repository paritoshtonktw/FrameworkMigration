using System;

namespace CryptoTrading.Models.Entities;

public class PriceHistory
{
    public int PriceHistoryId { get; set; }
    public int CryptocurrencyId { get; set; }
    public string Symbol { get; set; } = null!;
    public decimal Price { get; set; }
    public DateTime RecordedDate { get; set; }
}
