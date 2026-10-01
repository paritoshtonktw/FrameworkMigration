using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using CryptoTrading.Business.Services;
using CryptoTrading.Models.Requests;

namespace CryptoTrading.Infrastructure.Sqs
{
    public interface IOrderExecutionProcessor
    {
        Task<OrderExecutedEvent> ProcessOrderAsync(OrderPlacedEvent order);
    }

    public class OrderExecutionProcessor : IOrderExecutionProcessor
    {
        private readonly Func<ITradingService> _tradingServiceFactory;
        private readonly ISqsPublisher _publisher;
        private readonly SqsConfig _config;
        private readonly ILogger<OrderExecutionProcessor> _logger;

        public OrderExecutionProcessor(
            Func<ITradingService> tradingServiceFactory,
            ISqsPublisher publisher,
            IServiceProvider serviceProvider,
            ILogger<OrderExecutionProcessor> logger)
        {
            _tradingServiceFactory = tradingServiceFactory ?? throw new ArgumentNullException(nameof(tradingServiceFactory));
            _publisher = publisher;
            _config = SqsConfig.FromConfiguration(serviceProvider.GetRequiredService<IConfiguration>());
            _logger = logger;
        }

        public async Task<OrderExecutedEvent> ProcessOrderAsync(OrderPlacedEvent order)
        {
            if (order == null) throw new ArgumentNullException(nameof(order));

            _logger.LogInformation("[Sqs:OrderExecutionProcessor] Processing Order {CorrelationId}: {Side} {Quantity} {Symbol} for User {UserId}", order.CorrelationId, order.Side, order.Quantity, order.Symbol, order.UserId);

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

                _logger.LogInformation("[Sqs:OrderExecutionProcessor] Successfully executed Order {CorrelationId} -> Trade {TradeId} @ ${ExecutedPrice}", order.CorrelationId, executedEvent.TradeId, executedEvent.ExecutedPrice);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Sqs:OrderExecutionProcessor] Order {CorrelationId} execution failed: {Message}", order.CorrelationId, ex.Message);
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
