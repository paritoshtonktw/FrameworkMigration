namespace CryptoTrading.Models.DTOs;

public class PortfolioSummaryDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = null!;
    public decimal CashBalance { get; set; }
    public decimal InvestedValue { get; set; }
    public decimal HoldingsMarketValue { get; set; }
    public decimal TotalPortfolioValue { get; set; }
    public decimal UnrealizedProfitLoss { get; set; }
    public decimal RealizedProfitLoss { get; set; }
    public decimal TotalProfitLoss { get; set; }
}
