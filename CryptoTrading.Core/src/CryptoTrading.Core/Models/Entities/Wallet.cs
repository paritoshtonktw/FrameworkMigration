using System;

namespace CryptoTrading.Models.Entities;

public class Wallet
{
    public int WalletId { get; set; }
    public int UserId { get; set; }
    public string Currency { get; set; } = null!;
    public string? CurrencyName { get; set; }
    public decimal Quantity { get; set; }
    public decimal AverageCost { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
}
