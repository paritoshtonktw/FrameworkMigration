using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using CryptoTrading.Business.Services;
using CryptoTrading.Infrastructure.Sqs;
using CryptoTrading.Models.Requests;

namespace CryptoTrading.Core.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/orders")]
    public class OrdersController : BaseApiController
    {
        private readonly IOrderService _orderService;
        private readonly ITradingService _tradingService;
        private readonly ISqsPublisher? _sqsPublisher;
        private readonly SqsConfig _sqsConfig;

        public OrdersController(
            IOrderService orderService, 
            ITradingService tradingService, 
            IConfiguration configuration,
            ISqsPublisher? sqsPublisher = null)
        {
            _orderService = orderService;
            _tradingService = tradingService;
            _sqsPublisher = sqsPublisher;
            _sqsConfig = SqsConfig.FromConfiguration(configuration);
        }

        [HttpGet("")]
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

        [HttpPost("")]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request, [FromQuery] bool async = false)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var correlationId = Guid.NewGuid();
            var symbol = (request.Symbol ?? "").Trim().ToUpperInvariant();

            // 1. Publish OrderPlacedEvent to AWS SQS queue with MessageGroupId = Symbol (strict FIFO per coin)
            if (_sqsPublisher != null && _sqsPublisher.IsActive)
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
                    await _sqsPublisher.PublishAsync(_sqsConfig.OrdersIncomingQueueUrl, symbol, orderEvent);
                }
                catch (Exception sqsEx)
                {
                    System.Diagnostics.Trace.WriteLine($"[SQS] Warning publishing order event: {sqsEx.Message}");
                }
            }

            // 2. If client requests asynchronous queuing (< 10ms response for AWS high-scale trading)
            if (async)
            {
                return StatusCode((int)HttpStatusCode.Accepted, new
                {
                    success = true,
                    correlationId = correlationId,
                    status = "QUEUED",
                    symbol = symbol,
                    side = request.Side,
                    quantity = request.Quantity,
                    message = "Order queued to AWS SQS queue for matching engine execution."
                });
            }

            // 3. Synchronous matching execution path (Default for UI and immediate confirmation)
            if (string.Equals(request.Side, "BUY", StringComparison.OrdinalIgnoreCase))
            {
                var buyResult = await _tradingService.BuyAsync(CurrentUserId, new BuyTradeRequest
                {
                    Symbol = request.Symbol ?? string.Empty,
                    Quantity = request.Quantity,
                    OrderType = request.OrderType ?? "MARKET"
                });
                return EnvelopeCreated($"api/orders/{buyResult.OrderId}", buyResult, "Buy order executed successfully.");
            }
            else if (string.Equals(request.Side, "SELL", StringComparison.OrdinalIgnoreCase))
            {
                var sellResult = await _tradingService.SellAsync(CurrentUserId, new SellTradeRequest
                {
                    Symbol = request.Symbol ?? string.Empty,
                    Quantity = request.Quantity,
                    OrderType = request.OrderType ?? "MARKET"
                });
                return EnvelopeCreated($"api/orders/{sellResult.OrderId}", sellResult, "Sell order executed successfully.");
            }

            return BadRequest("Invalid order side. Must be BUY or SELL.");
        }

        [HttpPost("{id:int}/cancel")]
        public async Task<IActionResult> CancelOrder(int id)
        {
            var cancelledOrder = await _orderService.CancelOrderAsync(id, CurrentUserId);
            return EnvelopeOk(cancelledOrder, "Order cancelled successfully.");
        }
    }
}
