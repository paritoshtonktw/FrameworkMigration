using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CryptoTrading.Business.Services;

namespace CryptoTrading.Core.Controllers
{
    [ApiController]
    [Route("api/cryptocurrencies")]
    public class CryptocurrenciesController : BaseApiController
    {
        private readonly ICryptoService _cryptoService;

        public CryptocurrenciesController(ICryptoService cryptoService)
        {
            _cryptoService = cryptoService;
        }

        [HttpGet("")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll([FromQuery] bool force = false)
        {
            var cryptos = await _cryptoService.GetCryptocurrenciesAsync(force);
            Response.Headers.CacheControl = "no-cache, no-store, must-revalidate";
            return EnvelopeOk(cryptos);
        }

        [HttpGet("{symbol}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetBySymbol(string symbol)
        {
            var crypto = await _cryptoService.GetCryptocurrencyBySymbolAsync(symbol);
            if (crypto == null)
                return NotFound();

            Response.Headers.CacheControl = "no-cache, no-store, must-revalidate";
            return EnvelopeOk(crypto);
        }

        [HttpGet("{symbol}/history")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPriceHistory(string symbol, [FromQuery] int limit = 100)
        {
            var history = await _cryptoService.GetPriceHistoryAsync(symbol, limit);
            return EnvelopeOk(history);
        }

        [HttpGet("{symbol}/chart")]
        [AllowAnonymous]
        public async Task<IActionResult> GetChart(string symbol, [FromQuery] string timeframe = "24h")
        {
            var chart = await _cryptoService.GetMarketChartAsync(symbol, timeframe);
            Response.Headers.CacheControl = "no-cache, no-store, must-revalidate";
            return EnvelopeOk(chart);
        }
    }
}
