using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using CryptoTrading.Business.Services;

namespace CryptoTrading.Core.Controllers;

[Route("api/cryptocurrencies")]
[AllowAnonymous]
public class CryptocurrenciesController : BaseApiController
{
    private readonly ICryptoService _cryptoService;

    public CryptocurrenciesController(ICryptoService cryptoService)
    {
        _cryptoService = cryptoService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool force = false)
    {
        var cryptos = await _cryptoService.GetCryptocurrenciesAsync(force);
        SetNoCacheHeader();
        return EnvelopeOk(cryptos);
    }

    [HttpGet("{symbol}")]
    public async Task<IActionResult> GetBySymbol(string symbol)
    {
        var crypto = await _cryptoService.GetCryptocurrencyBySymbolAsync(symbol);
        if (crypto == null)
            return NotFound(new { success = false, message = $"Cryptocurrency with symbol '{symbol}' not found.", errorCode = "NOT_FOUND" });

        SetNoCacheHeader();
        return EnvelopeOk(crypto);
    }

    [HttpGet("{symbol}/history")]
    public async Task<IActionResult> GetPriceHistory(string symbol, [FromQuery] int limit = 100)
    {
        var history = await _cryptoService.GetPriceHistoryAsync(symbol, limit);
        return EnvelopeOk(history);
    }

    [HttpGet("{symbol}/chart")]
    public async Task<IActionResult> GetChart(string symbol, [FromQuery] string timeframe = "24h")
    {
        var chart = await _cryptoService.GetMarketChartAsync(symbol, timeframe);
        SetNoCacheHeader();
        return EnvelopeOk(chart);
    }

    private void SetNoCacheHeader()
    {
        Response.Headers[HeaderNames.CacheControl] = "no-cache, no-store, must-revalidate";
        Response.Headers[HeaderNames.Pragma] = "no-cache";
        Response.Headers[HeaderNames.Expires] = "0";
    }
}
