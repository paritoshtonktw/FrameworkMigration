using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CryptoTrading.Business.Services;

namespace CryptoTrading.Core.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/portfolio")]
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

        [HttpGet("")]
        public async Task<IActionResult> GetPortfolio()
        {
            var portfolio = await _portfolioService.GetPortfolioAsync(CurrentUserId);
            return EnvelopeOk(portfolio);
        }

        [HttpGet("holdings")]
        public async Task<IActionResult> GetHoldings()
        {
            var holdings = await _portfolioService.GetHoldingsAsync(CurrentUserId);
            return EnvelopeOk(holdings);
        }

        [HttpGet("performance")]
        public async Task<IActionResult> GetPerformance()
        {
            var performance = await _portfolioService.GetPerformanceAsync(CurrentUserId);
            return EnvelopeOk(performance);
        }

        [HttpPost("positions/{symbol}/close")]
        public async Task<IActionResult> ClosePosition(string symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol))
                return BadRequest("Cryptocurrency symbol is required.");

            try
            {
                var trade = await _tradingService.ClosePositionAsync(CurrentUserId, symbol);
                return EnvelopeOk(trade, $"Position for {symbol.ToUpperInvariant()} closed successfully at ${trade.ExecutionPrice:N2}.");
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
