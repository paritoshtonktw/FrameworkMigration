using System;

namespace CryptoTrading.Models.DTOs;

public class HoldingDto
{
    public int WalletId { get; set; }
    public int UserId { get; set; }
    public string Symbol { get; set; } = null!;
    public string? Name { get; set; }
    public decimal Quantity { get; set; }
    public decimal AverageCost { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal CurrentValue { get; set; }
    public decimal TotalCost { get; set; }
    public decimal UnrealizedProfitLoss { get; set; }
    public decimal UnrealizedProfitLossPercentage { get; set; }
    public DateTime UpdatedDate { get; set; }
}
