using System;

namespace CryptoTrading.Models.Entities;

public class Account
{
    public int AccountId { get; set; }
    public int UserId { get; set; }
    public string Currency { get; set; } = null!;
    public decimal AvailableBalance { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
}
