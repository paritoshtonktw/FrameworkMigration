using System;

namespace CryptoTrading.Models.DTOs;

public class TransactionDto
{
    public int TransactionId { get; set; }
    public int UserId { get; set; }
    public string TransactionType { get; set; } = null!;
    public string Currency { get; set; } = null!;
    public decimal Amount { get; set; }
    public string? ReferenceId { get; set; }
    public string Status { get; set; } = null!;
    public DateTime CreatedDate { get; set; }
    public decimal BalanceAfter { get; set; }
    public string Description => $"{TransactionType} transaction {(!string.IsNullOrEmpty(ReferenceId) ? $"ref #{ReferenceId}" : Status)}".Trim();
}
