using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CryptoTrading.Data.Repositories;
using CryptoTrading.Infrastructure.Logging;
using CryptoTrading.Models.DTOs;
using CryptoTrading.Models.Requests;

namespace CryptoTrading.Business.Services;

public interface IFinancialService
{
    Task<DepositDto> DepositAsync(int userId, DepositRequest request);
    Task<WithdrawalDto> WithdrawAsync(int userId, WithdrawalRequest request);
    Task<IEnumerable<TransactionDto>> GetTransactionsAsync(int userId, int limit = 100);
    Task<TransactionDto> GetTransactionByIdAsync(int transactionId, int userId);
    Task<IEnumerable<DepositDto>> GetDepositsAsync(int userId, int limit = 100);
    Task<IEnumerable<WithdrawalDto>> GetWithdrawalsAsync(int userId, int limit = 100);
}

public class FinancialService : IFinancialService
{
    private readonly IDepositRepository _depositRepo;
    private readonly IWithdrawalRepository _withdrawalRepo;
    private readonly ITransactionRepository _transactionRepo;
    private readonly ILoggerService _logger;

    public FinancialService(
        IDepositRepository depositRepo,
        IWithdrawalRepository withdrawalRepo,
        ITransactionRepository transactionRepo,
        ILoggerService logger)
    {
        _depositRepo = depositRepo;
        _withdrawalRepo = withdrawalRepo;
        _transactionRepo = transactionRepo;
        _logger = logger;
    }

    public async Task<DepositDto> DepositAsync(int userId, DepositRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        if (request.Amount <= 0)
            throw new ArgumentException("Deposit amount must be greater than zero.", nameof(request.Amount));

        _logger.Info($"Processing deposit for User #{userId}: ${request.Amount} {request.Currency}");
        var deposit = await _depositRepo.ProcessDepositAsync(userId, request.Amount, request.Currency ?? "USD");
        if (deposit == null)
            throw new InvalidOperationException("Deposit processing failed.");

        _logger.Info($"Deposit #{deposit.DepositId} processed successfully. New balance: ${deposit.NewBalance}");
        return deposit;
    }

    public async Task<WithdrawalDto> WithdrawAsync(int userId, WithdrawalRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        if (request.Amount <= 0)
            throw new ArgumentException("Withdrawal amount must be greater than zero.", nameof(request.Amount));

        _logger.Info($"Processing withdrawal for User #{userId}: ${request.Amount} {request.Currency}");
        var withdrawal = await _withdrawalRepo.ProcessWithdrawalAsync(userId, request.Amount, request.Currency ?? "USD");
        if (withdrawal == null)
            throw new InvalidOperationException("Withdrawal processing failed.");

        _logger.Info($"Withdrawal #{withdrawal.WithdrawalId} processed successfully. Remaining balance: ${withdrawal.RemainingBalance}");
        return withdrawal;
    }

    public async Task<IEnumerable<TransactionDto>> GetTransactionsAsync(int userId, int limit = 100)
    {
        return await _transactionRepo.GetTransactionsByUserAsync(userId, limit);
    }

    public async Task<TransactionDto> GetTransactionByIdAsync(int transactionId, int userId)
    {
        var tx = await _transactionRepo.GetTransactionByIdAsync(transactionId, userId);
        if (tx == null)
            throw new KeyNotFoundException($"Transaction #{transactionId} not found.");
        return tx;
    }

    public async Task<IEnumerable<DepositDto>> GetDepositsAsync(int userId, int limit = 100)
    {
        return await _depositRepo.GetDepositsByUserAsync(userId, limit);
    }

    public async Task<IEnumerable<WithdrawalDto>> GetWithdrawalsAsync(int userId, int limit = 100)
    {
        return await _withdrawalRepo.GetWithdrawalsByUserAsync(userId, limit);
    }
}
