using System;
using System.Collections.Generic;

namespace CryptoTrading.Models.DTOs;

public class CryptoChartDto
{
    public string Symbol { get; set; } = null!;
    public string Timeframe { get; set; } = null!;
    public decimal CurrentPrice { get; set; }
    public decimal PriceChange { get; set; }
    public decimal PriceChangePercentage { get; set; }
    public decimal HighPrice { get; set; }
    public decimal LowPrice { get; set; }
    public List<PricePointDto> Prices { get; set; } = new List<PricePointDto>();
}
