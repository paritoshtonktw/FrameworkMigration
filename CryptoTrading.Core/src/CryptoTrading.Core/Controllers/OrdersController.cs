using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

    public OrdersController(
        IOrderService orderService,
        ITradingService tradingService,
        IPubSubPublisher? pubSubPublisher = null)
    {
        _orderService = orderService;
        _tradingService = tradingService;
        _pubSubPublisher = pubSubPublisher;
    }

    [HttpGet]
    public async Task<IActionResult> GetOrders([FromQuery] int limit = 100)
    {
        int userId = CurrentUserId;
        var orders = await _orderService.GetOrdersAsync(userId, limit);
        return EnvelopeOk(orders);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetOrderById(int id)
    {
        int userId = CurrentUserId;
        var order = await _orderService.GetOrderByIdAsync(id, userId);
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
        if (_pubSubPublisher != null && _pubSubPublisher.IsActive)
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
                    Price = request.Price,
                    CreatedAt = DateTime.UtcNow
                };
                await _pubSubPublisher.PublishAsync("crypto-orders-incoming", symbol, orderEvent);
            }
            catch (Exception psEx)
            {
                Serilog.Log.Warning("Failed to publish order placed event: {Message}", psEx.Message);
            }
        }

        // 2. If client requests asynchronous queuing (HTTP 202 Accepted)
        if (async)
        {
            return Accepted(new
            {
                success = true,
                message = "Order queued to Google Cloud Pub/Sub stream for matching engine execution.",
                data = new
                {
                    correlationId = correlationId,
                    status = "QUEUED",
                    symbol = symbol,
                    side = request.Side,
                    quantity = request.Quantity
                }
            });
        }

        // 3. Synchronous matching execution path (Default for Immediate confirmation)
        if (string.Equals(request.Side, "BUY", StringComparison.OrdinalIgnoreCase))
        {
            var buyResult = await _tradingService.BuyAsync(CurrentUserId, new BuyTradeRequest
            {
                Symbol = request.Symbol ?? string.Empty,
                Quantity = request.Quantity,
                OrderType = request.OrderType ?? "MARKET"
            });
            return EnvelopeCreated($"/api/orders/{buyResult.OrderId}", buyResult, "Buy order executed successfully.");
        }
        else if (string.Equals(request.Side, "SELL", StringComparison.OrdinalIgnoreCase))
        {
            var sellResult = await _tradingService.SellAsync(CurrentUserId, new SellTradeRequest
            {
                Symbol = request.Symbol ?? string.Empty,
                Quantity = request.Quantity,
                OrderType = request.OrderType ?? "MARKET"
            });
            return EnvelopeCreated($"/api/orders/{sellResult.OrderId}", sellResult, "Sell order executed successfully.");
        }

        return BadRequest(new { success = false, message = "Invalid order side. Must be BUY or SELL.", errorCode = "INVALID_ARGUMENT" });
    }

    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> CancelOrder(int id)
    {
        int userId = CurrentUserId;
        var cancelledOrder = await _orderService.CancelOrderAsync(id, userId);
        return EnvelopeOk(cancelledOrder, "Order cancelled successfully.");
    }
}
