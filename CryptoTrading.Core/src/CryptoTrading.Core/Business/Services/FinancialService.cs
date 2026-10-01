using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using CryptoTrading.Data.Repositories;
using CryptoTrading.Models.DTOs;
using CryptoTrading.Models.Requests;

namespace CryptoTrading.Business.Services
{
    public interface IFinancialService
    {
        Task<DepositDto> DepositAsync(int userId, DepositRequest request);
        Task<WithdrawalDto> WithdrawAsync(int userId, WithdrawalRequest request);
        Task<List<TransactionDto>> GetTransactionsAsync(int userId, int limit = 100);
        Task<TransactionDto> GetTransactionByIdAsync(int transactionId, int userId);
        Task<List<DepositDto>> GetDepositsAsync(int userId, int limit = 100);
        Task<List<WithdrawalDto>> GetWithdrawalsAsync(int userId, int limit = 100);
    }

    public class FinancialService : IFinancialService
    {
        private readonly IDepositRepository _depositRepo;
        private readonly IWithdrawalRepository _withdrawalRepo;
        private readonly ITransactionRepository _transactionRepo;
        private readonly ILogger<FinancialService> _logger;

        public FinancialService(
            IDepositRepository depositRepo,
            IWithdrawalRepository withdrawalRepo,
            ITransactionRepository transactionRepo,
            ILogger<FinancialService> logger)
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

            _logger.LogInformation("Processing deposit for User #{UserId}: ${Amount} {Currency}", userId, request.Amount, request.Currency);
            var deposit = await _depositRepo.ProcessDepositAsync(userId, request.Amount, request.Currency ?? "USD");
            if (deposit == null)
            {
                throw new InvalidOperationException("Deposit processing failed.");
            }
            _logger.LogInformation("Deposit #{DepositId} processed successfully. New balance: ${NewBalance}", deposit.DepositId, deposit.NewBalance);
            return deposit;
        }

        public async Task<WithdrawalDto> WithdrawAsync(int userId, WithdrawalRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (request.Amount <= 0)
                throw new ArgumentException("Withdrawal amount must be greater than zero.", nameof(request.Amount));

            _logger.LogInformation("Processing withdrawal for User #{UserId}: ${Amount} {Currency}", userId, request.Amount, request.Currency);
            var withdrawal = await _withdrawalRepo.ProcessWithdrawalAsync(userId, request.Amount, request.Currency ?? "USD");
            if (withdrawal == null)
            {
                throw new InvalidOperationException("Withdrawal processing failed.");
            }
            _logger.LogInformation("Withdrawal #{WithdrawalId} processed successfully. Remaining balance: ${RemainingBalance}", withdrawal.WithdrawalId, withdrawal.RemainingBalance);
            return withdrawal;
        }

        public async Task<List<TransactionDto>> GetTransactionsAsync(int userId, int limit = 100)
        {
            var txs = await _transactionRepo.GetTransactionsByUserAsync(userId, limit);
            return txs.ToList();
        }

        public async Task<TransactionDto> GetTransactionByIdAsync(int transactionId, int userId)
        {
            var tx = await _transactionRepo.GetTransactionByIdAsync(transactionId, userId);
            if (tx == null)
                throw new KeyNotFoundException($"Transaction #{transactionId} not found.");
            return tx;
        }

        public async Task<List<DepositDto>> GetDepositsAsync(int userId, int limit = 100)
        {
            var deposits = await _depositRepo.GetDepositsByUserAsync(userId, limit);
            return deposits.ToList();
        }

        public async Task<List<WithdrawalDto>> GetWithdrawalsAsync(int userId, int limit = 100)
        {
            var withdrawals = await _withdrawalRepo.GetWithdrawalsByUserAsync(userId, limit);
            return withdrawals.ToList();
        }
    }
}
