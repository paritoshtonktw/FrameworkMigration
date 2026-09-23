using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Http;
using CryptoTrading.Business.Services;
using CryptoTrading.Web.Security;

namespace CryptoTrading.Web.Controllers
{
    [JwtAuthorize]
    [RoutePrefix("api/portfolio")]
    public class PortfolioController : BaseApiController
    {
        private readonly IPortfolioService _portfolioService;
        private readonly ITradingService _tradingService;

        public PortfolioController() : this(
            DependencyConfig.PortfolioService,
            DependencyConfig.TradingService)
        {
        }

        public PortfolioController(IPortfolioService portfolioService) : this(
            portfolioService,
            DependencyConfig.TradingService)
        {
        }

        public PortfolioController(
            IPortfolioService portfolioService,
            ITradingService tradingService)
        {
            _portfolioService = portfolioService;
            _tradingService = tradingService;
        }

        [HttpGet]
        [Route("")]
        public async Task<IHttpActionResult> GetPortfolio()
        {
            var portfolio = await _portfolioService.GetPortfolioAsync(CurrentUserId);
            return OkResponse(portfolio);
        }

        [HttpGet]
        [Route("holdings")]
        public async Task<IHttpActionResult> GetHoldings()
        {
            var holdings = await _portfolioService.GetHoldingsAsync(CurrentUserId);
            return OkResponse(holdings);
        }

        [HttpGet]
        [Route("performance")]
        public async Task<IHttpActionResult> GetPerformance()
        {
            var performance = await _portfolioService.GetPerformanceAsync(CurrentUserId);
            return OkResponse(performance);
        }

        [HttpPost]
        [Route("positions/{symbol}/close")]
        public async Task<IHttpActionResult> ClosePosition(string symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol))
                return BadRequest("Cryptocurrency symbol is required.");

            try
            {
                var trade = await _tradingService.ClosePositionAsync(CurrentUserId, symbol);
                return OkResponse(trade, $"Position for {symbol.ToUpperInvariant()} closed successfully at ${trade.ExecutionPrice:N2}.");
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}

