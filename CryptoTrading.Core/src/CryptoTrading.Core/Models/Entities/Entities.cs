using System;

namespace CryptoTrading.Models.Entities
{
    public class User
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public bool IsActive { get; set; }
    }

    public class Account
    {
        public int AccountId { get; set; }
        public int UserId { get; set; }
        public string Currency { get; set; } = "USD";
        public decimal AvailableBalance { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
    }

    public class Cryptocurrency
    {
        public int CryptocurrencyId { get; set; }
        public string Symbol { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal CurrentPrice { get; set; }
        public decimal PriceChange24h { get; set; }
        public DateTime LastUpdated { get; set; }
        public bool IsActive { get; set; }
    }

    public class Wallet
    {
        public int WalletId { get; set; }
        public int UserId { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string CurrencyName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal AverageCost { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
    }

    public class Order
    {
        public int OrderId { get; set; }
        public int UserId { get; set; }
        public string Symbol { get; set; } = string.Empty;
        public string OrderType { get; set; } = string.Empty;
        public string Side { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal TotalValue { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public DateTime? ExecutedDate { get; set; }
    }

    public class Trade
    {
        public int TradeId { get; set; }
        public int OrderId { get; set; }
        public int UserId { get; set; }
        public string Symbol { get; set; } = string.Empty;
        public string Side { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal ExecutionPrice { get; set; }
        public decimal TotalValue { get; set; }
        public decimal RealizedProfitLoss { get; set; }
        public DateTime ExecutedDate { get; set; }
    }

    public class TransactionRecord
    {
        public int TransactionId { get; set; }
        public int UserId { get; set; }
        public string TransactionType { get; set; } = string.Empty;
        public string Currency { get; set; } = "USD";
        public decimal Amount { get; set; }
        public string ReferenceId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
    }

    public class Deposit
    {
        public int DepositId { get; set; }
        public int UserId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "USD";
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public DateTime ProcessedDate { get; set; }
    }

    public class Withdrawal
    {
        public int WithdrawalId { get; set; }
        public int UserId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "USD";
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public DateTime ProcessedDate { get; set; }
    }

    public class PriceHistory
    {
        public int PriceHistoryId { get; set; }
        public int CryptocurrencyId { get; set; }
        public string Symbol { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public DateTime RecordedDate { get; set; }
    }
}
