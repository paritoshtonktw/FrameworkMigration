using System;

namespace CryptoTrading.Models.DTOs;

public class PricePointDto
{
    public long Timestamp { get; set; }
    public DateTime Date { get; set; }
    public decimal Price { get; set; }
}
