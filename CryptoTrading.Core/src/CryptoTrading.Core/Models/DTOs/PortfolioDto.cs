using System.Collections.Generic;

namespace CryptoTrading.Models.DTOs;

public class PortfolioDto
{
    public PortfolioSummaryDto Summary { get; set; } = new PortfolioSummaryDto();
    public List<HoldingDto> Holdings { get; set; } = new List<HoldingDto>();

    // Flat convenience properties matching Summary
    public decimal CashBalance => Summary?.CashBalance ?? 0m;
    public decimal HoldingsMarketValue => Summary?.HoldingsMarketValue ?? 0m;
    public decimal CryptoHoldingsValue => Summary?.HoldingsMarketValue ?? 0m;
    public decimal TotalPortfolioValue => Summary?.TotalPortfolioValue ?? 0m;
    public decimal UnrealizedProfitLoss => Summary?.UnrealizedProfitLoss ?? 0m;
    public decimal TotalUnrealizedPnL => Summary?.UnrealizedProfitLoss ?? 0m;
    public decimal RealizedProfitLoss => Summary?.RealizedProfitLoss ?? 0m;
    public decimal TotalRealizedPnL => Summary?.RealizedProfitLoss ?? 0m;
    public decimal TotalProfitLoss => Summary?.TotalProfitLoss ?? 0m;
}
