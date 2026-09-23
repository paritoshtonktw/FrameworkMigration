using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CryptoTrading.Business.Services;

namespace CryptoTrading.Core.Controllers;

[Route("api/portfolio")]
[Authorize]
public class PortfolioController : BaseApiController
{
    private readonly IPortfolioService _portfolioService;
    private readonly ITradingService _tradingService;

    public PortfolioController(
        IPortfolioService portfolioService,
        ITradingService tradingService)
    {
        _portfolioService = portfolioService;
        _tradingService = tradingService;
    }

    [HttpGet]
    public async Task<IActionResult> GetPortfolio()
    {
        int userId = CurrentUserId;
        var portfolio = await _portfolioService.GetPortfolioAsync(userId);
        return EnvelopeOk(portfolio);
    }

    [HttpGet("holdings")]
    public async Task<IActionResult> GetHoldings()
    {
        int userId = CurrentUserId;
        var holdings = await _portfolioService.GetHoldingsAsync(userId);
        return EnvelopeOk(holdings);
    }

    [HttpGet("performance")]
    public async Task<IActionResult> GetPerformance()
    {
        int userId = CurrentUserId;
        var performance = await _portfolioService.GetPerformanceAsync(userId);
        return EnvelopeOk(performance);
    }

    [HttpPost("positions/{symbol}/close")]
    public async Task<IActionResult> ClosePosition(string symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            return BadRequest(new { success = false, message = "Cryptocurrency symbol is required.", errorCode = "INVALID_ARGUMENT" });

        try
        {
            int userId = CurrentUserId;
            var trade = await _tradingService.ClosePositionAsync(userId, symbol);
            return EnvelopeOk(trade, $"Position for {symbol.ToUpperInvariant()} closed successfully at ${trade.ExecutionPrice:N2}.");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message, errorCode = "INVALID_OPERATION" });
        }
    }
}
