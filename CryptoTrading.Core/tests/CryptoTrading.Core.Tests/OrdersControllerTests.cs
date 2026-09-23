using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using NUnit.Framework;
using CryptoTrading.Business.Services;
using CryptoTrading.Core.Controllers;
using CryptoTrading.Infrastructure.PubSub;
using CryptoTrading.Models.DTOs;
using CryptoTrading.Models.Requests;

namespace CryptoTrading.Core.Tests;

[TestFixture]
public class OrdersControllerTests
{
    private Mock<IOrderService> _orderServiceMock = null!;
    private Mock<ITradingService> _tradingServiceMock = null!;
    private Mock<IPubSubPublisher> _pubSubPublisherMock = null!;
    private Mock<IConfiguration> _configMock = null!;
    private OrdersController _controller = null!;

    private const int TestUserId = 42;

    [SetUp]
    public void Setup()
    {
        _orderServiceMock = new Mock<IOrderService>();
        _tradingServiceMock = new Mock<ITradingService>();
        _pubSubPublisherMock = new Mock<IPubSubPublisher>();
        _configMock = new Mock<IConfiguration>();

        // Set up the controller with mocked dependencies
        _controller = new OrdersController(
            _orderServiceMock.Object,
            _tradingServiceMock.Object,
            _configMock.Object,
            _pubSubPublisherMock.Object
        );

        // Mock User Claims Principal to authenticate as TestUserId
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, TestUserId.ToString())
        }, "TestAuth"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    private static object? GetProperty(object obj, string name)
    {
        return obj.GetType().GetProperty(name)?.GetValue(obj);
    }

    [Test]
    public async Task GetOrders_ValidRequest_ReturnsOkWithOrders()
    {
        // Arrange
        var mockOrders = new List<OrderDto>
        {
            new() { OrderId = 1, UserId = TestUserId, Symbol = "BTC", Quantity = 0.5m, Price = 60000m, Status = "Completed" },
            new() { OrderId = 2, UserId = TestUserId, Symbol = "ETH", Quantity = 2.0m, Price = 3000m, Status = "Pending" }
        };

        _orderServiceMock.Setup(s => s.GetOrdersAsync(TestUserId, 100))
            .ReturnsAsync(mockOrders);

        // Act
        var result = await _controller.GetOrders(100);

        // Assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var okResult = (OkObjectResult)result;
        Assert.That(okResult.Value, Is.Not.Null);

        var value = okResult.Value!;
        Assert.That(GetProperty(value, "success"), Is.True);
        Assert.That(GetProperty(value, "data"), Is.EqualTo(mockOrders));
    }

    [Test]
    public async Task GetOrderById_ValidRequest_ReturnsOkWithOrder()
    {
        // Arrange
        var mockOrder = new OrderDto { OrderId = 10, UserId = TestUserId, Symbol = "BTC", Quantity = 0.5m, Price = 60000m, Status = "Completed" };

        _orderServiceMock.Setup(s => s.GetOrderByIdAsync(10, TestUserId))
            .ReturnsAsync(mockOrder);

        // Act
        var result = await _controller.GetOrderById(10);

        // Assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var okResult = (OkObjectResult)result;
        Assert.That(okResult.Value, Is.Not.Null);

        var value = okResult.Value!;
        Assert.That(GetProperty(value, "success"), Is.True);
        Assert.That(GetProperty(value, "data"), Is.EqualTo(mockOrder));
    }

    [Test]
    public async Task GetOrderById_NotFound_PropagatesKeyNotFoundException()
    {
        // Arrange
        _orderServiceMock.Setup(s => s.GetOrderByIdAsync(999, TestUserId))
            .ThrowsAsync(new KeyNotFoundException("Order not found."));

        // Act & Assert
        Assert.ThrowsAsync<KeyNotFoundException>(async () => await _controller.GetOrderById(999));
    }

    [Test]
    public async Task CreateOrder_AsyncTrue_PublishesEventAndReturnsAccepted()
    {
        // Arrange
        var req = new CreateOrderRequest
        {
            Symbol = "BTC",
            Side = "BUY",
            OrderType = "MARKET",
            Quantity = 0.5m,
            Price = null
        };

        _configMock.Setup(c => c.GetSection("PubSub:Enabled")).Returns(new Mock<IConfigurationSection>().Object); // Or let PubSubConfig handle defaults

        _pubSubPublisherMock.Setup(p => p.PublishAsync(It.IsAny<string>(), "BTC", It.IsAny<OrderPlacedEvent>(), null))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.CreateOrder(req, async: true);

        // Assert
        Assert.That(result, Is.InstanceOf<AcceptedResult>());
        var acceptedResult = (AcceptedResult)result;
        Assert.That(acceptedResult.Value, Is.Not.Null);

        var payload = acceptedResult.Value!;
        Assert.That(GetProperty(payload, "success"), Is.True);
        Assert.That(GetProperty(payload, "status"), Is.EqualTo("QUEUED"));
        Assert.That(GetProperty(payload, "symbol"), Is.EqualTo("BTC"));
        Assert.That(GetProperty(payload, "side"), Is.EqualTo("BUY"));
    }

    [Test]
    public async Task CreateOrder_SyncBuy_ExecutesBuyAndReturnsCreated()
    {
        // Arrange
        var req = new CreateOrderRequest
        {
            Symbol = "BTC",
            Side = "BUY",
            OrderType = "MARKET",
            Quantity = 0.5m,
            Price = null
        };

        var tradeDto = new TradeDto
        {
            TradeId = 101,
            OrderId = 1,
            UserId = TestUserId,
            Symbol = "BTC",
            Side = "BUY",
            Quantity = 0.5m,
            ExecutionPrice = 64000m,
            TotalValue = 32000m
        };

        _tradingServiceMock.Setup(s => s.BuyAsync(TestUserId, It.Is<BuyTradeRequest>(r => r.Symbol == "BTC" && r.Quantity == 0.5m)))
            .ReturnsAsync(tradeDto);

        // Act
        var result = await _controller.CreateOrder(req, async: false);

        // Assert
        Assert.That(result, Is.InstanceOf<CreatedResult>());
        var createdResult = (CreatedResult)result;
        Assert.That(createdResult.Location, Is.EqualTo("/api/trades/101"));
        Assert.That(createdResult.Value, Is.Not.Null);

        var envelope = createdResult.Value!;
        Assert.That(GetProperty(envelope, "success"), Is.True);
        Assert.That(GetProperty(envelope, "data"), Is.EqualTo(tradeDto));
    }

    [Test]
    public async Task CreateOrder_SyncSell_ExecutesSellAndReturnsCreated()
    {
        // Arrange
        var req = new CreateOrderRequest
        {
            Symbol = "BTC",
            Side = "SELL",
            OrderType = "MARKET",
            Quantity = 0.5m,
            Price = null
        };

        var tradeDto = new TradeDto
        {
            TradeId = 102,
            OrderId = 2,
            UserId = TestUserId,
            Symbol = "BTC",
            Side = "SELL",
            Quantity = 0.5m,
            ExecutionPrice = 64000m,
            TotalValue = 32000m
        };

        _tradingServiceMock.Setup(s => s.SellAsync(TestUserId, It.Is<SellTradeRequest>(r => r.Symbol == "BTC" && r.Quantity == 0.5m)))
            .ReturnsAsync(tradeDto);

        // Act
        var result = await _controller.CreateOrder(req, async: false);

        // Assert
        Assert.That(result, Is.InstanceOf<CreatedResult>());
        var createdResult = (CreatedResult)result;
        Assert.That(createdResult.Location, Is.EqualTo("/api/trades/102"));
        Assert.That(createdResult.Value, Is.Not.Null);

        var envelope = createdResult.Value!;
        Assert.That(GetProperty(envelope, "success"), Is.True);
        Assert.That(GetProperty(envelope, "data"), Is.EqualTo(tradeDto));
    }

    [Test]
    public async Task CancelOrder_ValidRequest_ReturnsOkWithCancelledOrder()
    {
        // Arrange
        var mockOrder = new OrderDto { OrderId = 15, UserId = TestUserId, Symbol = "BTC", Quantity = 0.5m, Price = 60000m, Status = "Cancelled" };

        _orderServiceMock.Setup(s => s.CancelOrderAsync(15, TestUserId))
            .ReturnsAsync(mockOrder);

        // Act
        var result = await _controller.CancelOrder(15);

        // Assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var okResult = (OkObjectResult)result;
        Assert.That(okResult.Value, Is.Not.Null);

        var envelope = okResult.Value!;
        Assert.That(GetProperty(envelope, "success"), Is.True);
        Assert.That(GetProperty(envelope, "data"), Is.EqualTo(mockOrder));
        Assert.That(GetProperty(envelope, "message"), Is.EqualTo("Order cancelled successfully."));
    }
}
