using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using CryptoTrading.Business.Services;
using CryptoTrading.Data.Repositories;
using CryptoTrading.Models.Entities;
using CryptoTrading.Models.Requests;

namespace CryptoTrading.Core.Tests;

[TestFixture]
public class UserTests
{
    private Mock<IUserRepository> _userRepoMock = null!;
    private Mock<IAccountRepository> _accountRepoMock = null!;
    private UserService _userService = null!;

    [SetUp]
    public void Setup()
    {
        _userRepoMock = new Mock<IUserRepository>();
        _accountRepoMock = new Mock<IAccountRepository>();

        _userService = new UserService(_userRepoMock.Object, _accountRepoMock.Object);
    }

    [Test]
    public async Task GetUserProfileAsync_UserExists_ReturnsUserProfile()
    {
        // Arrange
        var user = new User
        {
            UserId = 1,
            Username = "testtrader",
            Email = "test@trading.com",
            FirstName = "Test",
            LastName = "Trader",
            CreatedDate = DateTime.UtcNow,
            IsActive = true
        };

        var account = new Account
        {
            AccountId = 12,
            UserId = 1,
            Currency = "USD",
            AvailableBalance = 1500.50m
        };

        _userRepoMock.Setup(r => r.GetUserByIdAsync(1)).ReturnsAsync(user);
        _accountRepoMock.Setup(r => r.GetAccountAsync(1, "USD")).ReturnsAsync(account);

        // Act
        var res = await _userService.GetUserProfileAsync(1);

        // Assert
        Assert.That(res, Is.Not.Null);
        Assert.That(res.UserId, Is.EqualTo(1));
        Assert.That(res.Username, Is.EqualTo("testtrader"));
        Assert.That(res.CashBalance, Is.EqualTo(1500.50m));
        Assert.That(res.AccountId, Is.EqualTo(12));
    }

    [Test]
    public void GetUserProfileAsync_UserDoesNotExist_ThrowsKeyNotFoundException()
    {
        // Arrange
        _userRepoMock.Setup(r => r.GetUserByIdAsync(99)).ReturnsAsync((User?)null);

        // Act & Assert
        Assert.ThrowsAsync<KeyNotFoundException>(async () => await _userService.GetUserProfileAsync(99));
    }

    [Test]
    public async Task UpdateUserProfileAsync_ValidRequest_UpdatesAndReturnsProfile()
    {
        // Arrange
        var req = new UpdateProfileRequest
        {
            FirstName = "UpdatedFirstName",
            LastName = "UpdatedLastName",
            Email = "updated@trading.com"
        };

        var updatedUser = new User
        {
            UserId = 1,
            Username = "testtrader",
            Email = "updated@trading.com",
            FirstName = "UpdatedFirstName",
            LastName = "UpdatedLastName",
            CreatedDate = DateTime.UtcNow,
            IsActive = true
        };

        var account = new Account
        {
            AccountId = 12,
            UserId = 1,
            Currency = "USD",
            AvailableBalance = 1500.50m
        };

        _userRepoMock.Setup(r => r.UpdateUserAsync(1, "UpdatedFirstName", "UpdatedLastName", "updated@trading.com"))
            .ReturnsAsync(updatedUser);
        _accountRepoMock.Setup(r => r.GetAccountAsync(1, "USD")).ReturnsAsync(account);

        // Act
        var res = await _userService.UpdateUserProfileAsync(1, req);

        // Assert
        Assert.That(res, Is.Not.Null);
        Assert.That(res.FirstName, Is.EqualTo("UpdatedFirstName"));
        Assert.That(res.LastName, Is.EqualTo("UpdatedLastName"));
        Assert.That(res.Email, Is.EqualTo("updated@trading.com"));
        Assert.That(res.CashBalance, Is.EqualTo(1500.50m));
    }
}
