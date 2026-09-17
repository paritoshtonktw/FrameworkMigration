using System.Collections.Generic;

namespace CryptoTrading.Models.DTOs;

public class PortfolioDto
{
    public PortfolioSummaryDto Summary { get; set; } = new PortfolioSummaryDto();
    public List<HoldingDto> Holdings { get; set; } = new List<HoldingDto>();
}
