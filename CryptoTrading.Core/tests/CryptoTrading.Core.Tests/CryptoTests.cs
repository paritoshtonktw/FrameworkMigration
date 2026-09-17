using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using CryptoTrading.Business.Services;
using CryptoTrading.Data.Repositories;
using CryptoTrading.Infrastructure.MarketData;
using CryptoTrading.Models.DTOs;
using CryptoTrading.Models.Entities;

namespace CryptoTrading.Core.Tests;

[TestFixture]
public class CryptoTests
{
    private Mock<ICryptoMarketService> _marketServiceMock = null!;
    private Mock<ICryptocurrencyRepository> _cryptoRepoMock = null!;
    private CryptoService _cryptoService = null!;

    [SetUp]
    public void Setup()
    {
        _marketServiceMock = new Mock<ICryptoMarketService>();
        _cryptoRepoMock = new Mock<ICryptocurrencyRepository>();

        _cryptoService = new CryptoService(_marketServiceMock.Object, _cryptoRepoMock.Object);
    }

    [Test]
    public async Task GetCryptocurrenciesAsync_ReturnsCryptoList()
    {
        // Arrange
        var list = new List<CryptocurrencyDto>
        {
            new() { CryptocurrencyId = 1, Symbol = "BTC", Name = "Bitcoin", CurrentPrice = 64000m, PriceChange24h = 1.2m, IsActive = true },
            new() { CryptocurrencyId = 2, Symbol = "ETH", Name = "Ethereum", CurrentPrice = 3400m, PriceChange24h = -0.5m, IsActive = true }
        };

        _marketServiceMock.Setup(m => m.GetMarketCryptocurrenciesAsync(false))
            .ReturnsAsync(list);

        // Act
        var res = await _cryptoService.GetCryptocurrenciesAsync(false);

        // Assert
        Assert.That(res, Is.Not.Null);
        Assert.That(res.Count(), Is.EqualTo(2));
        Assert.That(res.First().Symbol, Is.EqualTo("BTC"));
    }

    [Test]
    public async Task GetCryptocurrencyBySymbolAsync_ReturnsCorrectCrypto()
    {
        // Arrange
        var cryptoDto = new CryptocurrencyDto { CryptocurrencyId = 1, Symbol = "BTC", Name = "Bitcoin", CurrentPrice = 64000m, PriceChange24h = 1.2m, IsActive = true };

        _marketServiceMock.Setup(m => m.GetCryptocurrencyPriceAsync("BTC", false))
            .ReturnsAsync(cryptoDto);

        // Act
        var res = await _cryptoService.GetCryptocurrencyBySymbolAsync("BTC");

        // Assert
        Assert.That(res, Is.Not.Null);
        Assert.That(res.Symbol, Is.EqualTo("BTC"));
        Assert.That(res.CurrentPrice, Is.EqualTo(64000m));
    }

    [Test]
    public async Task GetPriceHistoryAsync_ReturnsPriceHistory()
    {
        // Arrange
        var history = new List<PriceHistory>
        {
            new() { PriceHistoryId = 10, CryptocurrencyId = 1, Symbol = "BTC", Price = 63900m, RecordedDate = DateTime.UtcNow.AddMinutes(-10) },
            new() { PriceHistoryId = 11, CryptocurrencyId = 1, Symbol = "BTC", Price = 64000m, RecordedDate = DateTime.UtcNow }
        };

        _cryptoRepoMock.Setup(r => r.GetCryptoPriceHistoryAsync("BTC", 50))
            .ReturnsAsync(history);

        // Act
        var res = await _cryptoService.GetPriceHistoryAsync("BTC", 50);

        // Assert
        Assert.That(res, Is.Not.Null);
        Assert.That(res.Count(), Is.EqualTo(2));
        Assert.That(res.First().Price, Is.EqualTo(63900m));
    }

    [Test]
    public async Task GetMarketChartAsync_ReturnsChartData()
    {
        // Arrange
        var chartDto = new CryptoChartDto
        {
            Symbol = "BTC",
            Timeframe = "24H",
            CurrentPrice = 64000m,
            HighPrice = 64500m,
            LowPrice = 63500m,
            Prices = new List<PricePointDto>
            {
                new() { Timestamp = 1600000000000, Date = DateTime.UtcNow.AddHours(-1), Price = 63500m },
                new() { Timestamp = 1600000003600, Date = DateTime.UtcNow, Price = 64000m }
            }
        };

        _marketServiceMock.Setup(m => m.GetMarketChartAsync("BTC", "24h"))
            .ReturnsAsync(chartDto);

        // Act
        var res = await _cryptoService.GetMarketChartAsync("BTC", "24h");

        // Assert
        Assert.That(res, Is.Not.Null);
        Assert.That(res.Symbol, Is.EqualTo("BTC"));
        Assert.That(res.HighPrice, Is.EqualTo(64500m));
        Assert.That(res.Prices.Count, Is.EqualTo(2));
    }
}
