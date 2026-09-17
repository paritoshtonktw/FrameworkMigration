using System;

namespace CryptoTrading.Models.DTOs;

public class TradeDto
{
    public int TradeId { get; set; }
    public int OrderId { get; set; }
    public int UserId { get; set; }
    public string Symbol { get; set; } = null!;
    public string Side { get; set; } = null!;
    public decimal Quantity { get; set; }
    public decimal ExecutionPrice { get; set; }
    public decimal TotalValue { get; set; }
    public decimal RealizedProfitLoss { get; set; }
    public DateTime ExecutedDate { get; set; }
    public decimal RemainingBalance { get; set; }
    public decimal RemainingCryptoHolding { get; set; }
}
