using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using CryptoTrading.Data.Repositories;
using CryptoTrading.Infrastructure.Logging;
using CryptoTrading.Infrastructure.Reports;
using CryptoTrading.Models.DTOs;
using CryptoTrading.Models.Entities;

namespace CryptoTrading.Core.Tests;

[TestFixture]
public class PdfReportServiceTests
{
    private Mock<IUserRepository> _userRepoMock = null!;
    private Mock<IPortfolioRepository> _portfolioRepoMock = null!;
    private Mock<ITradingRepository> _tradingRepoMock = null!;
    private Mock<ITransactionRepository> _txRepoMock = null!;
    private Mock<ILoggerService> _loggerMock = null!;
    private PdfReportService _reportService = null!;

    private const int TestUserId = 42;

    [SetUp]
    public void Setup()
    {
        _userRepoMock = new Mock<IUserRepository>();
        _portfolioRepoMock = new Mock<IPortfolioRepository>();
        _tradingRepoMock = new Mock<ITradingRepository>();
        _txRepoMock = new Mock<ITransactionRepository>();
        _loggerMock = new Mock<ILoggerService>();

        _reportService = new PdfReportService(
            _userRepoMock.Object,
            _portfolioRepoMock.Object,
            _tradingRepoMock.Object,
            _txRepoMock.Object,
            _loggerMock.Object
        );
    }

    [Test]
    public async Task GeneratePnLAndSettlementReportAsync_GeneratesValidPdfBytes_For30dTimeframe()
    {
        // Arrange
        var mockUser = new User { UserId = TestUserId, Username = "traderJoe", Email = "joe@example.com" };
        _userRepoMock.Setup(r => r.GetUserByIdAsync(TestUserId))
            .ReturnsAsync(mockUser);

        var mockPortfolio = new PortfolioDto
        {
            Summary = new PortfolioSummaryDto
            {
                UserId = TestUserId,
                Username = "traderJoe",
                CashBalance = 10000m,
                HoldingsMarketValue = 5000m,
                TotalPortfolioValue = 15000m,
                UnrealizedProfitLoss = 500m
            },
            Holdings = new List<HoldingDto>
            {
                new() { Symbol = "BTC", Name = "Bitcoin", Quantity = 0.1m, AverageCost = 45000m, CurrentPrice = 50000m, CurrentValue = 5000m, UnrealizedProfitLoss = 500m, UnrealizedProfitLossPercentage = 11.1m }
            }
        };
        _portfolioRepoMock.Setup(r => r.GetPortfolioAsync(TestUserId))
            .ReturnsAsync(mockPortfolio);

        var mockTrades = new List<TradeDto>
        {
            new() { TradeId = 1, Symbol = "BTC", Side = "BUY", Quantity = 0.05m, ExecutionPrice = 40000m, TotalValue = 2000m, ExecutedDate = DateTime.UtcNow.AddDays(-5) },
            new() { TradeId = 2, Symbol = "BTC", Side = "SELL", Quantity = 0.02m, ExecutionPrice = 45000m, TotalValue = 900m, RealizedProfitLoss = 100m, ExecutedDate = DateTime.UtcNow.AddDays(-3) }
        };
        _tradingRepoMock.Setup(r => r.GetTradesByUserAsync(TestUserId, 1000))
            .ReturnsAsync(mockTrades);

        var mockTransactions = new List<TransactionDto>
        {
            new() { TransactionId = 1, TransactionType = "DEPOSIT", Currency = "USD", Amount = 5000m, Status = "COMPLETED", CreatedDate = DateTime.UtcNow.AddDays(-10) },
            new() { TransactionId = 2, TransactionType = "WITHDRAWAL", Currency = "USD", Amount = 1000m, Status = "COMPLETED", CreatedDate = DateTime.UtcNow.AddDays(-8) }
        };
        _txRepoMock.Setup(r => r.GetTransactionsByUserAsync(TestUserId, 1000))
            .ReturnsAsync(mockTransactions);

        // Act
        var pdfBytes = await _reportService.GeneratePnLAndSettlementReportAsync(TestUserId, "30d");

        // Assert
        Assert.That(pdfBytes, Is.Not.Null);
        Assert.That(pdfBytes.Length, Is.GreaterThan(100));

        // Read PDF Header signature
        var header = Encoding.ASCII.GetString(pdfBytes.Take(5).ToArray());
        Assert.That(header, Is.EqualTo("%PDF-"));

        // Read EOF signature
        var footer = Encoding.ASCII.GetString(pdfBytes.Skip(pdfBytes.Length - 10).ToArray());
        Assert.That(footer, Does.Contain("%%EOF"));

        _loggerMock.Verify(l => l.Info(It.Is<string>(s => s.Contains("Generating PDF PnL"))), Times.Once);
    }

    [TestCase("7d")]
    [TestCase("90d")]
    [TestCase("ytd")]
    [TestCase("all")]
    [TestCase("invalid-timeframe")]
    public async Task GeneratePnLAndSettlementReportAsync_HandlesAllTimeframesAndGeneratesPdf(string timeframe)
    {
        // Arrange
        _userRepoMock.Setup(r => r.GetUserByIdAsync(TestUserId))
            .ReturnsAsync((User)null!);
        _portfolioRepoMock.Setup(r => r.GetPortfolioAsync(TestUserId))
            .ReturnsAsync((PortfolioDto)null!);
        _tradingRepoMock.Setup(r => r.GetTradesByUserAsync(TestUserId, It.IsAny<int>()))
            .ReturnsAsync((List<TradeDto>)null!);
        _txRepoMock.Setup(r => r.GetTransactionsByUserAsync(TestUserId, It.IsAny<int>()))
            .ReturnsAsync((List<TransactionDto>)null!);

        // Act
        var pdfBytes = await _reportService.GeneratePnLAndSettlementReportAsync(TestUserId, timeframe);

        // Assert
        Assert.That(pdfBytes, Is.Not.Null);
        Assert.That(pdfBytes.Length, Is.GreaterThan(100));
        var header = Encoding.ASCII.GetString(pdfBytes.Take(5).ToArray());
        Assert.That(header, Is.EqualTo("%PDF-"));
    }
}
