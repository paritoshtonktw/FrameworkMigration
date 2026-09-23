using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using CryptoTrading.Business.Services;
using CryptoTrading.Data.Repositories;
using CryptoTrading.Infrastructure.Logging;
using CryptoTrading.Infrastructure.MarketData;
using CryptoTrading.Models.DTOs;
using CryptoTrading.Models.Entities;
using CryptoTrading.Models.Requests;

namespace CryptoTrading.Core.Tests;

[TestFixture]
public class TradingAndPortfolioTests
{
    private Mock<ITradingRepository> _tradingRepoMock = null!;
    private Mock<ICryptoMarketService> _marketServiceMock = null!;
    private Mock<IWalletRepository> _walletRepoMock = null!;
    private Mock<ILoggerService> _loggerMock = null!;
    private Mock<IPortfolioRepository> _portfolioRepoMock = null!;
    private Mock<IUserRepository> _userRepoMock = null!;
    private Mock<ITransactionRepository> _txRepoMock = null!;

    private TradingService _tradingService = null!;
    private PortfolioService _portfolioService = null!;

    [SetUp]
    public void Setup()
    {
        _tradingRepoMock = new Mock<ITradingRepository>();
        _marketServiceMock = new Mock<ICryptoMarketService>();
        _walletRepoMock = new Mock<IWalletRepository>();
        _loggerMock = new Mock<ILoggerService>();
        _portfolioRepoMock = new Mock<IPortfolioRepository>();
        _userRepoMock = new Mock<IUserRepository>();
        _txRepoMock = new Mock<ITransactionRepository>();

        _tradingService = new TradingService(
            _tradingRepoMock.Object,
            _marketServiceMock.Object,
            _loggerMock.Object,
            _walletRepoMock.Object
        );

        _portfolioService = new PortfolioService(_portfolioRepoMock.Object);
    }

    [Test]
    public async Task BuyAsync_ValidRequest_ExecutesBuyAndReturnsTrade()
    {
        // Arrange
        var req = new BuyTradeRequest { Symbol = "BTC", Quantity = 0.5m, OrderType = "MARKET" };
        var cryptoDto = new CryptocurrencyDto { Symbol = "BTC", CurrentPrice = 64000m };
        var tradeDto = new TradeDto { TradeId = 500, OrderId = 120, UserId = 1, Symbol = "BTC", Side = "BUY", Quantity = 0.5m, ExecutionPrice = 64000m, TotalValue = 32000m };

        _marketServiceMock.Setup(m => m.GetCryptocurrencyPriceAsync("BTC", false))
            .ReturnsAsync(cryptoDto);
        _tradingRepoMock.Setup(t => t.ExecuteBuyOrderAsync(1, "BTC", 0.5m, 64000m))
            .ReturnsAsync(tradeDto);

        // Act
        var res = await _tradingService.BuyAsync(1, req);

        // Assert
        Assert.That(res, Is.Not.Null);
        Assert.That(res.TradeId, Is.EqualTo(500));
        Assert.That(res.TotalValue, Is.EqualTo(32000m));
    }

    [Test]
    public async Task SellAsync_ValidRequest_ExecutesSellAndReturnsTrade()
    {
        // Arrange
        var req = new SellTradeRequest { Symbol = "BTC", Quantity = 0.5m, OrderType = "MARKET" };
        var cryptoDto = new CryptocurrencyDto { Symbol = "BTC", CurrentPrice = 64000m };
        var tradeDto = new TradeDto { TradeId = 501, OrderId = 121, UserId = 1, Symbol = "BTC", Side = "SELL", Quantity = 0.5m, ExecutionPrice = 64000m, TotalValue = 32000m, RealizedProfitLoss = 2000m };

        _marketServiceMock.Setup(m => m.GetCryptocurrencyPriceAsync("BTC", false))
            .ReturnsAsync(cryptoDto);
        _tradingRepoMock.Setup(t => t.ExecuteSellOrderAsync(1, "BTC", 0.5m, 64000m))
            .ReturnsAsync(tradeDto);

        // Act
        var res = await _tradingService.SellAsync(1, req);

        // Assert
        Assert.That(res, Is.Not.Null);
        Assert.That(res.TradeId, Is.EqualTo(501));
        Assert.That(res.RealizedProfitLoss, Is.EqualTo(2000m));
    }

    [Test]
    public async Task ClosePositionAsync_OpenPosition_SellsEntireHolding()
    {
        // Arrange
        var wallet = new Wallet { WalletId = 5, UserId = 1, Currency = "SOL", Quantity = 10m, AverageCost = 130m };
        var cryptoDto = new CryptocurrencyDto { Symbol = "SOL", CurrentPrice = 150m };
        var tradeDto = new TradeDto { TradeId = 502, OrderId = 122, UserId = 1, Symbol = "SOL", Side = "SELL", Quantity = 10m, ExecutionPrice = 150m, TotalValue = 1500m, RealizedProfitLoss = 200m };

        _walletRepoMock.Setup(w => w.GetWalletHoldingAsync(1, "SOL"))
            .ReturnsAsync(wallet);
        _marketServiceMock.Setup(m => m.GetCryptocurrencyPriceAsync("SOL", false))
            .ReturnsAsync(cryptoDto);
        _tradingRepoMock.Setup(t => t.ExecuteSellOrderAsync(1, "SOL", 10m, 150m))
            .ReturnsAsync(tradeDto);

        // Act
        var res = await _tradingService.ClosePositionAsync(1, "SOL");

        // Assert
        Assert.That(res, Is.Not.Null);
        Assert.That(res.TradeId, Is.EqualTo(502));
        Assert.That(res.Quantity, Is.EqualTo(10m));
        Assert.That(res.RealizedProfitLoss, Is.EqualTo(200m));
    }

    [Test]
    public async Task GetPortfolioAsync_ReturnsPortfolioDto()
    {
        // Arrange
        var portfolioDto = new PortfolioDto
        {
            Summary = new PortfolioSummaryDto { UserId = 1, Username = "trader", TotalPortfolioValue = 25000m },
            Holdings = new List<HoldingDto> { new() { Symbol = "BTC", Quantity = 0.25m, CurrentValue = 16000m } }
        };

        _portfolioRepoMock.Setup(p => p.GetPortfolioAsync(1))
            .ReturnsAsync(portfolioDto);

        // Act
        var res = await _portfolioService.GetPortfolioAsync(1);

        // Assert
        Assert.That(res, Is.Not.Null);
        Assert.That(res.Summary.TotalPortfolioValue, Is.EqualTo(25000m));
        Assert.That(res.Holdings.Count, Is.EqualTo(1));
    }
}
