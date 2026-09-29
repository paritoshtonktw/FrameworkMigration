using System;
using System.Threading.Tasks;
using CryptoTrading.Business.Services;
using CryptoTrading.Infrastructure.Logging;
using CryptoTrading.Models.Requests;

namespace CryptoTrading.Infrastructure.Sqs
{
    /// <summary>
    /// Executes incoming orders atomically via TradingService (sp_ExecuteTrade)
    /// and publishes completion events to AWS SQS (crypto-orders-executed).
    /// Used by both SQS Webhook receivers and background subscribers.
    /// </summary>
    public class OrderExecutionProcessor : IOrderExecutionProcessor
    {
        private readonly Func<ITradingService> _tradingServiceFactory;
        private readonly ISqsPublisher _publisher;
        private readonly SqsConfig _config;
        private readonly ILoggerService _logger;

        public OrderExecutionProcessor(
            Func<ITradingService> tradingServiceFactory,
            ISqsPublisher publisher,
            SqsConfig config,
            ILoggerService logger)
        {
            _tradingServiceFactory = tradingServiceFactory ?? throw new ArgumentNullException(nameof(tradingServiceFactory));
            _publisher = publisher;
            _config = config ?? SqsConfig.FromConfiguration();
            _logger = logger;
        }

        public async Task<OrderExecutedEvent> ProcessOrderAsync(OrderPlacedEvent order)
        {
            if (order == null) throw new ArgumentNullException(nameof(order));

            _logger?.Info($"[Sqs:OrderExecutionProcessor] Processing Order {order.CorrelationId}: {order.Side} {order.Quantity} {order.Symbol} for User {order.UserId}");

            var executedEvent = new OrderExecutedEvent
            {
                CorrelationId = order.CorrelationId,
                UserId = order.UserId,
                Symbol = order.Symbol,
                Side = order.Side,
                ExecutedQuantity = order.Quantity,
                ExecutedAt = DateTime.UtcNow
            };

            try
            {
                var tradingService = _tradingServiceFactory();
                if (string.Equals(order.Side, "BUY", StringComparison.OrdinalIgnoreCase))
                {
                    var buyResult = await tradingService.BuyAsync(order.UserId, new BuyTradeRequest
                    {
                        Symbol = order.Symbol,
                        Quantity = order.Quantity,
                        OrderType = order.OrderType
                    });

                    executedEvent.Status = "FILLED";
                    executedEvent.TradeId = buyResult.TradeId;
                    executedEvent.ExecutedPrice = buyResult.Price;
                    executedEvent.TotalAmount = buyResult.TotalAmount;
                }
                else
                {
                    var sellResult = await tradingService.SellAsync(order.UserId, new SellTradeRequest
                    {
                        Symbol = order.Symbol,
                        Quantity = order.Quantity,
                        OrderType = order.OrderType
                    });

                    executedEvent.Status = "FILLED";
                    executedEvent.TradeId = sellResult.TradeId;
                    executedEvent.ExecutedPrice = sellResult.Price;
                    executedEvent.TotalAmount = sellResult.TotalAmount;
                }

                _logger?.Info($"[Sqs:OrderExecutionProcessor] Successfully executed Order {order.CorrelationId} -> Trade {executedEvent.TradeId} @ ${executedEvent.ExecutedPrice}");
            }
            catch (Exception ex)
            {
                _logger?.Error($"[Sqs:OrderExecutionProcessor] Order {order.CorrelationId} execution failed: {ex.Message}", ex);
                executedEvent.Status = "REJECTED";
                executedEvent.ErrorMessage = ex.Message;
            }

            // Publish execution result to crypto-orders-executed with MessageGroupId = Symbol
            if (_publisher != null && _publisher.IsActive)
            {
                await _publisher.PublishAsync(_config.OrdersExecutedQueueUrl, order.Symbol, executedEvent);
            }

            return executedEvent;
        }
    }
}
