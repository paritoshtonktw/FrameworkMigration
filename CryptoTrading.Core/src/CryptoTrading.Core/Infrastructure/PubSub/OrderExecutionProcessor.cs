using System;
using System.Threading.Tasks;
using CryptoTrading.Business.Services;
using CryptoTrading.Infrastructure.Logging;
using CryptoTrading.Models.Requests;

namespace CryptoTrading.Infrastructure.PubSub;

public interface IOrderExecutionProcessor
{
    Task<OrderExecutedEvent> ProcessOrderAsync(OrderPlacedEvent order);
}

public class OrderExecutionProcessor : IOrderExecutionProcessor
{
    private readonly Func<ITradingService> _tradingServiceFactory;
    private readonly IPubSubPublisher _publisher;
    private readonly ILoggerService _logger;

    public OrderExecutionProcessor(
        Func<ITradingService> tradingServiceFactory,
        IPubSubPublisher publisher,
        ILoggerService logger)
    {
        _tradingServiceFactory = tradingServiceFactory ?? throw new ArgumentNullException(nameof(tradingServiceFactory));
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<OrderExecutedEvent> ProcessOrderAsync(OrderPlacedEvent order)
    {
        if (order == null) throw new ArgumentNullException(nameof(order));

        _logger.Info($"[PubSub:OrderExecutionProcessor] Processing Order {order.CorrelationId}: {order.Side} {order.Quantity} {order.Symbol} for User {order.UserId}");

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
                executedEvent.ExecutedPrice = buyResult.ExecutionPrice;
                executedEvent.TotalAmount = buyResult.TotalValue;
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
                executedEvent.ExecutedPrice = sellResult.ExecutionPrice;
                executedEvent.TotalAmount = sellResult.TotalValue;
            }

            _logger.Info($"[PubSub:OrderExecutionProcessor] Successfully executed Order {order.CorrelationId} -> Trade {executedEvent.TradeId} @ ${executedEvent.ExecutedPrice}");
        }
        catch (Exception ex)
        {
            _logger.Error($"[PubSub:OrderExecutionProcessor] Order {order.CorrelationId} execution failed: {ex.Message}", ex);
            executedEvent.Status = "REJECTED";
            executedEvent.ErrorMessage = ex.Message;
        }

        // Publish execution result to crypto-orders-executed with OrderingKey = Symbol
        if (_publisher != null && _publisher.IsActive)
        {
            await _publisher.PublishAsync("crypto-orders-executed", order.Symbol, executedEvent);
        }

        return executedEvent;
    }
}
