using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using CryptoTrading.Business.Services;
using CryptoTrading.Data.Repositories;
using CryptoTrading.Infrastructure.Logging;
using CryptoTrading.Models.DTOs;
using CryptoTrading.Models.Requests;

namespace CryptoTrading.Core.Tests;

[TestFixture]
public class FinancialTests
{
    private Mock<IDepositRepository> _depositRepoMock = null!;
    private Mock<IWithdrawalRepository> _withdrawalRepoMock = null!;
    private Mock<ITransactionRepository> _transactionRepoMock = null!;
    private Mock<ILoggerService> _loggerMock = null!;
    private FinancialService _financialService = null!;

    [SetUp]
    public void Setup()
    {
        _depositRepoMock = new Mock<IDepositRepository>();
        _withdrawalRepoMock = new Mock<IWithdrawalRepository>();
        _transactionRepoMock = new Mock<ITransactionRepository>();
        _loggerMock = new Mock<ILoggerService>();

        _financialService = new FinancialService(
            _depositRepoMock.Object,
            _withdrawalRepoMock.Object,
            _transactionRepoMock.Object,
            _loggerMock.Object
        );
    }

    [Test]
    public async Task DepositAsync_ValidAmount_CallsRepositoryAndIncreasesBalance()
    {
        // Arrange
        var req = new DepositRequest { Amount = 5000m, Currency = "USD" };
        var depositDto = new DepositDto
        {
            DepositId = 50,
            UserId = 1,
            Amount = 5000m,
            Currency = "USD",
            Status = "COMPLETED",
            NewBalance = 15000m
        };

        _depositRepoMock.Setup(d => d.ProcessDepositAsync(1, 5000m, "USD"))
            .ReturnsAsync(depositDto);

        // Act
        var res = await _financialService.DepositAsync(1, req);

        // Assert
        Assert.That(res, Is.Not.Null);
        Assert.That(res.Amount, Is.EqualTo(5000m));
        Assert.That(res.NewBalance, Is.EqualTo(15000m));
    }

    [Test]
    public void DepositAsync_NegativeAmount_ThrowsArgumentException()
    {
        // Arrange
        var req = new DepositRequest { Amount = -100m, Currency = "USD" };

        // Act & Assert
        Assert.ThrowsAsync<ArgumentException>(async () =>
            await _financialService.DepositAsync(1, req)
        );
    }

    [Test]
    public async Task WithdrawAsync_ValidAmount_CallsRepositoryAndDecreasesBalance()
    {
        // Arrange
        var req = new WithdrawalRequest { Amount = 2000m, Currency = "USD" };
        var withdrawalDto = new WithdrawalDto
        {
            WithdrawalId = 60,
            UserId = 1,
            Amount = 2000m,
            Currency = "USD",
            Status = "COMPLETED",
            RemainingBalance = 8000m,
            NewBalance = 8000m
        };

        _withdrawalRepoMock.Setup(w => w.ProcessWithdrawalAsync(1, 2000m, "USD"))
            .ReturnsAsync(withdrawalDto);

        // Act
        var res = await _financialService.WithdrawAsync(1, req);

        // Assert
        Assert.That(res, Is.Not.Null);
        Assert.That(res.Amount, Is.EqualTo(2000m));
        Assert.That(res.RemainingBalance, Is.EqualTo(8000m));
    }

    [Test]
    public void WithdrawAsync_NegativeAmount_ThrowsArgumentException()
    {
        // Arrange
        var req = new WithdrawalRequest { Amount = -50m, Currency = "USD" };

        // Act & Assert
        Assert.ThrowsAsync<ArgumentException>(async () =>
            await _financialService.WithdrawAsync(1, req)
        );
    }

    [Test]
    public async Task GetTransactionsAsync_ReturnsTransactionsList()
    {
        // Arrange
        var list = new List<TransactionDto>
        {
            new() { TransactionId = 101, UserId = 1, TransactionType = "DEPOSIT", Currency = "USD", Amount = 500m, Status = "COMPLETED" },
            new() { TransactionId = 102, UserId = 1, TransactionType = "WITHDRAWAL", Currency = "USD", Amount = 100m, Status = "COMPLETED" }
        };

        _transactionRepoMock.Setup(t => t.GetTransactionsByUserAsync(1, 10))
            .ReturnsAsync(list);

        // Act
        var res = await _financialService.GetTransactionsAsync(1, 10);

        // Assert
        Assert.That(res, Is.Not.Null);
        Assert.That(res, Is.EquivalentTo(list));
    }

    [Test]
    public async Task GetTransactionByIdAsync_ReturnsTransactionDetails()
    {
        // Arrange
        var txDto = new TransactionDto { TransactionId = 101, UserId = 1, TransactionType = "DEPOSIT", Currency = "USD", Amount = 500m, Status = "COMPLETED" };

        _transactionRepoMock.Setup(t => t.GetTransactionByIdAsync(101, 1))
            .ReturnsAsync(txDto);

        // Act
        var res = await _financialService.GetTransactionByIdAsync(101, 1);

        // Assert
        Assert.That(res, Is.Not.Null);
        Assert.That(res.TransactionId, Is.EqualTo(101));
    }
}
