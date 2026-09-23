using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using CryptoTrading.Business.Services;
using CryptoTrading.Infrastructure.PubSub;
using CryptoTrading.Models.Requests;

namespace CryptoTrading.Core.Controllers;

[Route("api/orders")]
[Authorize]
public class OrdersController : BaseApiController
{
    private readonly IOrderService _orderService;
    private readonly ITradingService _tradingService;
    private readonly IPubSubPublisher? _pubSubPublisher;
    private readonly IConfiguration _configuration;

    public OrdersController(
        IOrderService orderService,
        ITradingService tradingService,
        IConfiguration configuration,
        IPubSubPublisher? pubSubPublisher = null)
    {
        _orderService = orderService;
        _tradingService = tradingService;
        _configuration = configuration;
        _pubSubPublisher = pubSubPublisher;
    }

    [HttpGet]
    public async Task<IActionResult> GetOrders([FromQuery] int limit = 100)
    {
        var orders = await _orderService.GetOrdersAsync(CurrentUserId, limit);
        return EnvelopeOk(orders);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetOrderById(int id)
    {
        var order = await _orderService.GetOrderByIdAsync(id, CurrentUserId);
        return EnvelopeOk(order);
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request, [FromQuery] bool async = false)
    {
        if (request == null)
            return BadRequest(new { success = false, message = "Payload cannot be empty.", errorCode = "INVALID_ARGUMENT" });

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var correlationId = Guid.NewGuid();
        var symbol = (request.Symbol ?? "").Trim().ToUpperInvariant();

        // 1. Publish OrderPlacedEvent to Google Cloud Pub/Sub topic with OrderingKey = Symbol (strict FIFO per coin)
        if (_pubSubPublisher != null)
        {
            try
            {
                var orderEvent = new OrderPlacedEvent
                {
                    CorrelationId = correlationId,
                    UserId = CurrentUserId,
                    Symbol = symbol,
                    Side = (request.Side ?? "").ToUpperInvariant(),
                    OrderType = (request.OrderType ?? "MARKET").ToUpperInvariant(),
                    Quantity = request.Quantity,
                    Price = request.Price ?? 0m,
                    CreatedAt = DateTime.UtcNow
                };
                var pubSubConfig = PubSubConfig.FromConfiguration(_configuration);
                await _pubSubPublisher.PublishAsync(pubSubConfig.OrdersIncomingTopic, symbol, orderEvent);
            }
            catch (Exception psEx)
            {
                System.Diagnostics.Trace.WriteLine($"[PubSub] Warning publishing order event: {psEx.Message}");
            }
        }

        // 2. If client requests asynchronous queuing (< 10ms response for GCP high-scale trading)
        if (async)
        {
            return Accepted(new
            {
                success = true,
                correlationId = correlationId,
                status = "QUEUED",
                symbol = symbol,
                side = request.Side,
                quantity = request.Quantity,
                message = "Order queued to Google Cloud Pub/Sub stream for matching engine execution."
            });
        }

        // 3. Synchronous matching execution path (Default for UI and immediate confirmation)
        if (string.Equals(request.Side, "BUY", StringComparison.OrdinalIgnoreCase))
        {
            var buyResult = await _tradingService.BuyAsync(CurrentUserId, new BuyTradeRequest
            {
                Symbol = symbol,
                Quantity = request.Quantity,
                OrderType = request.OrderType ?? "MARKET"
            });
            return EnvelopeCreated($"/api/trades/{buyResult.TradeId}", buyResult, "Buy order executed successfully.");
        }
        else if (string.Equals(request.Side, "SELL", StringComparison.OrdinalIgnoreCase))
        {
            var sellResult = await _tradingService.SellAsync(CurrentUserId, new SellTradeRequest
            {
                Symbol = symbol,
                Quantity = request.Quantity,
                OrderType = request.OrderType ?? "MARKET"
            });
            return EnvelopeCreated($"/api/trades/{sellResult.TradeId}", sellResult, "Sell order executed successfully.");
        }

        return BadRequest(new { success = false, message = "Invalid order side. Must be BUY or SELL.", errorCode = "INVALID_ARGUMENT" });
    }

    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> CancelOrder(int id)
    {
        var cancelledOrder = await _orderService.CancelOrderAsync(id, CurrentUserId);
        return EnvelopeOk(cancelledOrder, "Order cancelled successfully.");
    }
}
