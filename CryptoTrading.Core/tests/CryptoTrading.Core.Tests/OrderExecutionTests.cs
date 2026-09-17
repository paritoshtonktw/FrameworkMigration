using System;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using CryptoTrading.Business.Services;
using CryptoTrading.Infrastructure.Logging;
using CryptoTrading.Infrastructure.PubSub;
using CryptoTrading.Models.DTOs;
using CryptoTrading.Models.Requests;

namespace CryptoTrading.Core.Tests;

[TestFixture]
public class OrderExecutionTests
{
    private Mock<ITradingService> _tradingServiceMock = null!;
    private Mock<IPubSubPublisher> _publisherMock = null!;
    private Mock<ILoggerService> _loggerMock = null!;
    private OrderExecutionProcessor _processor = null!;

    [SetUp]
    public void Setup()
    {
        _tradingServiceMock = new Mock<ITradingService>();
        _publisherMock = new Mock<IPubSubPublisher>();
        _loggerMock = new Mock<ILoggerService>();

        _processor = new OrderExecutionProcessor(
            () => _tradingServiceMock.Object,
            _publisherMock.Object,
            _loggerMock.Object
        );
    }

    [Test]
    public async Task ProcessOrderAsync_BuyOrderPlaced_ExecutesBuyAndReturnsFilled()
    {
        // Arrange
        var correlationId = Guid.NewGuid();
        var placedEvent = new OrderPlacedEvent
        {
            CorrelationId = correlationId,
            UserId = 1,
            Symbol = "BTC",
            Side = "BUY",
            OrderType = "MARKET",
            Quantity = 0.25m
        };

        var tradeDto = new TradeDto
        {
            TradeId = 801,
            OrderId = 201,
            UserId = 1,
            Symbol = "BTC",
            Side = "BUY",
            Quantity = 0.25m,
            ExecutionPrice = 64000m,
            TotalValue = 16000m
        };

        _tradingServiceMock.Setup(s => s.BuyAsync(1, It.Is<BuyTradeRequest>(r => r.Symbol == "BTC" && r.Quantity == 0.25m)))
            .ReturnsAsync(tradeDto);

        // Act
        var res = await _processor.ProcessOrderAsync(placedEvent);

        // Assert
        Assert.That(res, Is.Not.Null);
        Assert.That(res.Status, Is.EqualTo("FILLED"));
        Assert.That(res.TradeId, Is.EqualTo(801));
        Assert.That(res.ExecutedPrice, Is.EqualTo(64000m));
        Assert.That(res.TotalAmount, Is.EqualTo(16000m));
    }

    [Test]
    public async Task ProcessOrderAsync_SellOrderPlaced_ExecutesSellAndReturnsFilled()
    {
        // Arrange
        var correlationId = Guid.NewGuid();
        var placedEvent = new OrderPlacedEvent
        {
            CorrelationId = correlationId,
            UserId = 1,
            Symbol = "ETH",
            Side = "SELL",
            OrderType = "MARKET",
            Quantity = 2m
        };

        var tradeDto = new TradeDto
        {
            TradeId = 802,
            OrderId = 202,
            UserId = 1,
            Symbol = "ETH",
            Side = "SELL",
            Quantity = 2m,
            ExecutionPrice = 3400m,
            TotalValue = 6800m
        };

        _tradingServiceMock.Setup(s => s.SellAsync(1, It.Is<SellTradeRequest>(r => r.Symbol == "ETH" && r.Quantity == 2m)))
            .ReturnsAsync(tradeDto);

        // Act
        var res = await _processor.ProcessOrderAsync(placedEvent);

        // Assert
        Assert.That(res, Is.Not.Null);
        Assert.That(res.Status, Is.EqualTo("FILLED"));
        Assert.That(res.TradeId, Is.EqualTo(802));
        Assert.That(res.ExecutedPrice, Is.EqualTo(3400m));
        Assert.That(res.TotalAmount, Is.EqualTo(6800m));
    }

    [Test]
    public async Task ProcessOrderAsync_ExecutionFails_ReturnsRejectedState()
    {
        // Arrange
        var correlationId = Guid.NewGuid();
        var placedEvent = new OrderPlacedEvent
        {
            CorrelationId = correlationId,
            UserId = 1,
            Symbol = "BTC",
            Side = "BUY",
            OrderType = "MARKET",
            Quantity = 1000m // Insufficient funds or invalid size
        };

        _tradingServiceMock.Setup(s => s.BuyAsync(1, It.IsAny<BuyTradeRequest>()))
            .ThrowsAsync(new InvalidOperationException("Insufficient cash balance."));

        // Act
        var res = await _processor.ProcessOrderAsync(placedEvent);

        // Assert
        Assert.That(res, Is.Not.Null);
        Assert.That(res.Status, Is.EqualTo("REJECTED"));
        Assert.That(res.ErrorMessage, Is.EqualTo("Insufficient cash balance."));
    }
}
