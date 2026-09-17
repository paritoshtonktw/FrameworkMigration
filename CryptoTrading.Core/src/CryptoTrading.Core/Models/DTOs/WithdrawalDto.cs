using System;

namespace CryptoTrading.Models.DTOs;

public class WithdrawalDto
{
    public int WithdrawalId { get; set; }
    public int UserId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTime CreatedDate { get; set; }
    public DateTime ProcessedDate { get; set; }
    
    private decimal _balance;
    public decimal NewBalance { get => _balance; set => _balance = value; }
    public decimal RemainingBalance { get => _balance; set => _balance = value; }
}
