using System;

namespace CryptoTrading.Models.DTOs;

public class UserProfileDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public DateTime CreatedDate { get; set; }
    public DateTime? LastLoginDate { get; set; }
    public decimal CashBalance { get; set; }
    public int AccountId { get; set; }
    public string Currency { get; set; } = "USD";

    // Dual-compatibility nested models
    public UserNestedDto User => new()
    {
        UserId = UserId,
        Username = Username,
        Email = Email,
        FirstName = FirstName,
        LastName = LastName,
        CreatedDate = CreatedDate,
        LastLoginDate = LastLoginDate
    };

    public AccountNestedDto Account => new()
    {
        AccountId = AccountId,
        UserId = UserId,
        Currency = Currency,
        Balance = CashBalance,
        AvailableBalance = CashBalance
    };
}

public class UserNestedDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public DateTime CreatedDate { get; set; }
    public DateTime? LastLoginDate { get; set; }
}

public class AccountNestedDto
{
    public int AccountId { get; set; }
    public int UserId { get; set; }
    public string Currency { get; set; } = null!;
    public decimal Balance { get; set; }
    public decimal AvailableBalance { get; set; }
}
