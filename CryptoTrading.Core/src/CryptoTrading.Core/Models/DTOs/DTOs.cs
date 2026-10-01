using System;
using System.Collections.Generic;

namespace CryptoTrading.Models.DTOs
{
    public class UserDto
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public DateTime? LastLoginDate { get; set; }
    }

    public class CryptocurrencyDto
    {
        public int CryptocurrencyId { get; set; }
        public string Symbol { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal CurrentPrice { get; set; }
        public decimal PriceChange24h { get; set; }
        public DateTime LastUpdated { get; set; }
        public bool IsActive { get; set; }
    }

    public class PricePointDto
    {
        public long Timestamp { get; set; }
        public DateTime Date { get; set; }
        public decimal Price { get; set; }
    }

    public class CryptoChartDto
    {
        public string Symbol { get; set; } = string.Empty;
        public string Timeframe { get; set; } = string.Empty;
        public decimal CurrentPrice { get; set; }
        public decimal PriceChange { get; set; }
        public decimal PriceChangePercentage { get; set; }
        public decimal HighPrice { get; set; }
        public decimal LowPrice { get; set; }
        public List<PricePointDto> Prices { get; set; } = new List<PricePointDto>();
    }

    public class HoldingDto
    {
        public int WalletId { get; set; }
        public int UserId { get; set; }
        public string Symbol { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal AverageCost { get; set; }
        public decimal AverageBuyPrice => AverageCost;
        public decimal CurrentPrice { get; set; }
        public decimal CurrentValue { get; set; }
        public decimal TotalCost { get; set; }
        public decimal UnrealizedProfitLoss { get; set; }
        public decimal UnrealizedPnL => UnrealizedProfitLoss;
        public decimal UnrealizedProfitLossPercentage { get; set; }
        public decimal UnrealizedPnLPercentage => UnrealizedProfitLossPercentage;
        public DateTime UpdatedDate { get; set; }
    }

    public class PortfolioSummaryDto
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public decimal CashBalance { get; set; }
        public decimal InvestedValue { get; set; }
        public decimal HoldingsMarketValue { get; set; }
        public decimal CryptoHoldingsValue => HoldingsMarketValue;
        public decimal TotalPortfolioValue { get; set; }
        public decimal UnrealizedProfitLoss { get; set; }
        public decimal TotalUnrealizedPnL => UnrealizedProfitLoss;
        public decimal RealizedProfitLoss { get; set; }
        public decimal TotalRealizedPnL => RealizedProfitLoss;
        public decimal TotalProfitLoss { get; set; }
    }

    public class PortfolioDto
    {
        public PortfolioSummaryDto Summary { get; set; } = new PortfolioSummaryDto();
        public List<HoldingDto> Holdings { get; set; } = new List<HoldingDto>();

        // Flat convenience properties matching Summary
        public decimal CashBalance => Summary?.CashBalance ?? 0m;
        public decimal HoldingsMarketValue => Summary?.HoldingsMarketValue ?? 0m;
        public decimal CryptoHoldingsValue => Summary?.HoldingsMarketValue ?? 0m;
        public decimal TotalPortfolioValue => Summary?.TotalPortfolioValue ?? 0m;
        public decimal UnrealizedProfitLoss => Summary?.UnrealizedProfitLoss ?? 0m;
        public decimal TotalUnrealizedPnL => Summary?.UnrealizedProfitLoss ?? 0m;
        public decimal RealizedProfitLoss => Summary?.RealizedProfitLoss ?? 0m;
        public decimal TotalRealizedPnL => Summary?.RealizedProfitLoss ?? 0m;
        public decimal TotalProfitLoss => Summary?.TotalProfitLoss ?? 0m;
    }

    public class PortfolioPerformanceDto
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public decimal CashBalance { get; set; }
        public decimal InvestedValue { get; set; }
        public decimal HoldingsMarketValue { get; set; }
        public decimal TotalPortfolioValue { get; set; }
        public decimal UnrealizedProfitLoss { get; set; }
        public decimal RealizedProfitLoss { get; set; }
        public decimal TotalProfitLoss { get; set; }
        public decimal ReturnPercentage { get; set; }
        public decimal TotalDeposited { get; set; }
        public decimal TotalWithdrawn { get; set; }
        public int TotalTradesCount { get; set; }
    }

    public class OrderDto
    {
        public int OrderId { get; set; }
        public int UserId { get; set; }
        public string Symbol { get; set; } = string.Empty;
        public string OrderType { get; set; } = string.Empty;
        public string Side { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal ExecutedPrice => Price;
        public decimal TotalValue { get; set; }
        public decimal TotalAmount => TotalValue;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public DateTime? ExecutedDate { get; set; }
    }

    public class TradeDto
    {
        public int TradeId { get; set; }
        public int OrderId { get; set; }
        public int UserId { get; set; }
        public string Symbol { get; set; } = string.Empty;
        public string Side { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal ExecutionPrice { get; set; }
        public decimal Price => ExecutionPrice;
        public decimal ExecutedPrice => ExecutionPrice;
        public decimal TotalValue { get; set; }
        public decimal TotalAmount => TotalValue;
        public decimal RealizedProfitLoss { get; set; }
        public decimal RealizedPnL => RealizedProfitLoss;
        public string Status => "FILLED";
        public DateTime ExecutedDate { get; set; }
        public decimal RemainingBalance { get; set; }
        public decimal? RemainingCryptoHolding { get; set; }
    }

    public class TransactionDto
    {
        public int TransactionId { get; set; }
        public int UserId { get; set; }
        public string TransactionType { get; set; } = string.Empty;
        public string Currency { get; set; } = "USD";
        public decimal Amount { get; set; }
        public string ReferenceId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public decimal BalanceAfter { get; set; }
        public string Description => $"{TransactionType} transaction {(!string.IsNullOrEmpty(ReferenceId) ? $"ref #{ReferenceId}" : Status)}".Trim();
    }

    public class DepositDto
    {
        public int DepositId { get; set; }
        public int UserId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "USD";
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public DateTime ProcessedDate { get; set; }
        private decimal _balance;
        public decimal NewBalance { get => _balance; set => _balance = value; }
        public decimal RemainingBalance { get => _balance; set => _balance = value; }
    }

    public class WithdrawalDto
    {
        public int WithdrawalId { get; set; }
        public int UserId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "USD";
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public DateTime ProcessedDate { get; set; }
        private decimal _balance;
        public decimal NewBalance { get => _balance; set => _balance = value; }
        public decimal RemainingBalance { get => _balance; set => _balance = value; }
    }

    public class UserProfileDto
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public decimal CashBalance { get; set; }
        public int AccountId { get; set; }
        public string Currency { get; set; } = "USD";

        // Dual-compatibility nested models
        public UserNestedDto User => new UserNestedDto
        {
            UserId = UserId,
            Username = Username,
            Email = Email,
            FirstName = FirstName,
            LastName = LastName,
            CreatedDate = CreatedDate,
            LastLoginDate = LastLoginDate
        };

        public AccountNestedDto Account => new AccountNestedDto
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
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public DateTime? LastLoginDate { get; set; }
    }

    public class AccountNestedDto
    {
        public int AccountId { get; set; }
        public int UserId { get; set; }
        public string Currency { get; set; } = "USD";
        public decimal Balance { get; set; }
        public decimal AvailableBalance { get; set; }
    }
}
