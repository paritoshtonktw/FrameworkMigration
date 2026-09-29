using System;
using System.Net;
using System.Threading.Tasks;
using System.Web.Http;
using CryptoTrading.Business.Services;
using CryptoTrading.Infrastructure.Sqs;
using CryptoTrading.Models.Requests;
using CryptoTrading.Web.Security;

namespace CryptoTrading.Web.Controllers
{
    [JwtAuthorize]
    [RoutePrefix("api/orders")]
    public class OrdersController : BaseApiController
    {
        private readonly IOrderService _orderService;
        private readonly ITradingService _tradingService;
        private readonly ISqsPublisher _sqsPublisher;

        public OrdersController() : this(DependencyConfig.OrderService, DependencyConfig.TradingService, DependencyConfig.SqsPublisher)
        {
        }

        public OrdersController(IOrderService orderService, ITradingService tradingService, ISqsPublisher sqsPublisher = null)
        {
            _orderService = orderService;
            _tradingService = tradingService;
            _sqsPublisher = sqsPublisher;
        }

        [HttpGet]
        [Route("")]
        public async Task<IHttpActionResult> GetOrders([FromUri] int limit = 100)
        {
            var orders = await _orderService.GetOrdersAsync(CurrentUserId, limit);
            return OkResponse(orders);
        }

        [HttpGet]
        [Route("{id:int}")]
        public async Task<IHttpActionResult> GetOrderById(int id)
        {
            var order = await _orderService.GetOrderByIdAsync(id, CurrentUserId);
            return OkResponse(order);
        }

        [HttpPost]
        [Route("")]
        public async Task<IHttpActionResult> CreateOrder([FromBody] CreateOrderRequest request, [FromUri] bool async = false)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var correlationId = Guid.NewGuid();
            var symbol = (request.Symbol ?? "").Trim().ToUpperInvariant();

            // 1. Publish OrderPlacedEvent to AWS SQS queue with MessageGroupId = Symbol (strict FIFO per coin)
            if (_sqsPublisher != null)
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
                    await _sqsPublisher.PublishAsync(SqsConfig.FromConfiguration().OrdersIncomingQueueUrl, symbol, orderEvent);
                }
                catch (Exception sqsEx)
                {
                    System.Diagnostics.Trace.WriteLine($"[SQS] Warning publishing order event: {sqsEx.Message}");
                }
            }

            // 2. If client requests asynchronous queuing (< 10ms response for AWS high-scale trading)
            if (async)
            {
                return Content(HttpStatusCode.Accepted, new
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
                    Symbol = request.Symbol,
                    Quantity = request.Quantity,
                    OrderType = request.OrderType
                });
                return CreatedResponse(buyResult, "Buy order executed successfully.");
            }
            else if (string.Equals(request.Side, "SELL", StringComparison.OrdinalIgnoreCase))
            {
                var sellResult = await _tradingService.SellAsync(CurrentUserId, new SellTradeRequest
                {
                    Symbol = request.Symbol,
                    Quantity = request.Quantity,
                    OrderType = request.OrderType
                });
                return CreatedResponse(sellResult, "Sell order executed successfully.");
            }

            return BadRequest("Invalid order side. Must be BUY or SELL.");
        }

        [HttpPost]
        [Route("{id:int}/cancel")]
        public async Task<IHttpActionResult> CancelOrder(int id)
        {
            var cancelledOrder = await _orderService.CancelOrderAsync(id, CurrentUserId);
            return OkResponse(cancelledOrder, "Order cancelled successfully.");
        }
    }
}

