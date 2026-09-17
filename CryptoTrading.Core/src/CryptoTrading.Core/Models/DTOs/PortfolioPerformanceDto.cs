namespace CryptoTrading.Models.DTOs;

public class PortfolioPerformanceDto
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
    public decimal ReturnPercentage { get; set; }
    public decimal TotalDeposited { get; set; }
    public decimal TotalWithdrawn { get; set; }
    public int TotalTradesCount { get; set; }
}
