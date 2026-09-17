using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CryptoTrading.Business.Services;
using CryptoTrading.Models.Requests;

namespace CryptoTrading.Core.Controllers;

[Route("api/trades")]
[Authorize]
public class TradesController : BaseApiController
{
    private readonly ITradingService _tradingService;

    public TradesController(ITradingService tradingService)
    {
        _tradingService = tradingService;
    }

    [HttpPost("buy")]
    public async Task<IActionResult> Buy([FromBody] BuyTradeRequest request)
    {
        if (request == null)
            return BadRequest(new { success = false, message = "Payload cannot be empty.", errorCode = "INVALID_ARGUMENT" });

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var trade = await _tradingService.BuyAsync(CurrentUserId, request);
        return EnvelopeCreated($"/api/trades/{trade.TradeId}", trade, "Cryptocurrency purchase executed successfully.");
    }

    [HttpPost("sell")]
    public async Task<IActionResult> Sell([FromBody] SellTradeRequest request)
    {
        if (request == null)
            return BadRequest(new { success = false, message = "Payload cannot be empty.", errorCode = "INVALID_ARGUMENT" });

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var trade = await _tradingService.SellAsync(CurrentUserId, request);
        return EnvelopeCreated($"/api/trades/{trade.TradeId}", trade, "Cryptocurrency sale executed successfully.");
    }

    [HttpGet]
    public async Task<IActionResult> GetTrades([FromQuery] int limit = 100)
    {
        int userId = CurrentUserId;
        var trades = await _tradingService.GetUserTradesAsync(userId, limit);
        return EnvelopeOk(trades);
    }
}
