using System;

namespace CryptoTrading.Models.Entities;

public class Cryptocurrency
{
    public int CryptocurrencyId { get; set; }
    public string Symbol { get; set; } = null!;
    public string Name { get; set; } = null!;
    public decimal CurrentPrice { get; set; }
    public decimal PriceChange24h { get; set; }
    public DateTime LastUpdated { get; set; }
    public bool IsActive { get; set; }
}
