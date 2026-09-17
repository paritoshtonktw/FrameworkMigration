using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using CryptoTrading.Data;
using CryptoTrading.Models.DTOs;

namespace CryptoTrading.Data.Repositories;

public interface ITransactionRepository
{
    Task<int> CreateTransactionAsync(int userId, string transactionType, string currency, decimal amount, string? referenceId = null, string status = "COMPLETED");
    Task<IEnumerable<TransactionDto>> GetTransactionsByUserAsync(int userId, int limit = 100);
    Task<TransactionDto?> GetTransactionByIdAsync(int transactionId, int? userId = null);
}

public class TransactionRepository : ITransactionRepository
{
    private readonly IDbConnectionFactory _dbConnectionFactory;

    public TransactionRepository(IDbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    public async Task<int> CreateTransactionAsync(int userId, string transactionType, string currency, decimal amount, string? referenceId = null, string status = "COMPLETED")
    {
        using var connection = _dbConnectionFactory.CreateConnection();

        var parameters = new DynamicParameters();
        parameters.Add("@UserId", userId, DbType.Int32);
        parameters.Add("@TransactionType", transactionType, DbType.String, size: 20);
        parameters.Add("@Currency", currency, DbType.String, size: 10);
        parameters.Add("@Amount", amount, DbType.Decimal);
        parameters.Add("@ReferenceId", referenceId, DbType.String, size: 50);
        parameters.Add("@Status", status, DbType.String, size: 20);
        parameters.Add("@TransactionId", dbType: DbType.Int32, direction: ParameterDirection.Output);

        await connection.ExecuteAsync(
            "usp_CreateTransaction",
            parameters,
            commandType: CommandType.StoredProcedure
        );

        return parameters.Get<int>("@TransactionId");
    }

    public async Task<IEnumerable<TransactionDto>> GetTransactionsByUserAsync(int userId, int limit = 100)
    {
        using var connection = _dbConnectionFactory.CreateConnection();

        var parameters = new { UserId = userId, Limit = limit };

        return await connection.QueryAsync<TransactionDto>(
            "usp_GetTransactionsByUser",
            parameters,
            commandType: CommandType.StoredProcedure
        );
    }

    public async Task<TransactionDto?> GetTransactionByIdAsync(int transactionId, int? userId = null)
    {
        using var connection = _dbConnectionFactory.CreateConnection();

        var parameters = new { TransactionId = transactionId, UserId = userId };

        return await connection.QueryFirstOrDefaultAsync<TransactionDto>(
            "usp_GetTransactionById",
            parameters,
            commandType: CommandType.StoredProcedure
        );
    }
}

public interface IDepositRepository
{
    Task<DepositDto?> ProcessDepositAsync(int userId, decimal amount, string currency = "USD");
    Task<IEnumerable<DepositDto>> GetDepositsByUserAsync(int userId, int limit = 100);
}

public class DepositRepository : IDepositRepository
{
    private readonly IDbConnectionFactory _dbConnectionFactory;

    public DepositRepository(IDbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    public async Task<DepositDto?> ProcessDepositAsync(int userId, decimal amount, string currency = "USD")
    {
        using var connection = _dbConnectionFactory.CreateConnection();

        var parameters = new DynamicParameters();
        parameters.Add("@UserId", userId, DbType.Int32);
        parameters.Add("@Amount", amount, DbType.Decimal);
        parameters.Add("@Currency", currency, DbType.String, size: 10);
        parameters.Add("@DepositId", dbType: DbType.Int32, direction: ParameterDirection.Output);

        return await connection.QueryFirstOrDefaultAsync<DepositDto>(
            "usp_ProcessDeposit",
            parameters,
            commandType: CommandType.StoredProcedure
        );
    }

    public async Task<IEnumerable<DepositDto>> GetDepositsByUserAsync(int userId, int limit = 100)
    {
        using var connection = _dbConnectionFactory.CreateConnection();

        var parameters = new { UserId = userId, Limit = limit };

        return await connection.QueryAsync<DepositDto>(
            "usp_GetDepositsByUser",
            parameters,
            commandType: CommandType.StoredProcedure
        );
    }
}

public interface IWithdrawalRepository
{
    Task<WithdrawalDto?> ProcessWithdrawalAsync(int userId, decimal amount, string currency = "USD");
    Task<IEnumerable<WithdrawalDto>> GetWithdrawalsByUserAsync(int userId, int limit = 100);
}

public class WithdrawalRepository : IWithdrawalRepository
{
    private readonly IDbConnectionFactory _dbConnectionFactory;

    public WithdrawalRepository(IDbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    public async Task<WithdrawalDto?> ProcessWithdrawalAsync(int userId, decimal amount, string currency = "USD")
    {
        using var connection = _dbConnectionFactory.CreateConnection();

        var parameters = new DynamicParameters();
        parameters.Add("@UserId", userId, DbType.Int32);
        parameters.Add("@Amount", amount, DbType.Decimal);
        parameters.Add("@Currency", currency, DbType.String, size: 10);
        parameters.Add("@WithdrawalId", dbType: DbType.Int32, direction: ParameterDirection.Output);

        return await connection.QueryFirstOrDefaultAsync<WithdrawalDto>(
            "usp_ProcessWithdrawal",
            parameters,
            commandType: CommandType.StoredProcedure
        );
    }

    public async Task<IEnumerable<WithdrawalDto>> GetWithdrawalsByUserAsync(int userId, int limit = 100)
    {
        using var connection = _dbConnectionFactory.CreateConnection();

        var parameters = new { UserId = userId, Limit = limit };

        return await connection.QueryAsync<WithdrawalDto>(
            "usp_GetWithdrawalsByUser",
            parameters,
            commandType: CommandType.StoredProcedure
        );
    }
}
