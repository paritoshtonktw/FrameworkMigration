using System.Data;
using Dapper;
using CryptoTrading.Data;
using CryptoTrading.Models.Entities;

namespace CryptoTrading.Data.Repositories;

public interface IAccountRepository
{
    Task<int> CreateAccountAsync(int userId, string currency = "USD", decimal initialBalance = 0.00m);
    Task<Account?> GetAccountAsync(int userId, string currency = "USD");
    Task<decimal> GetAccountBalanceAsync(int userId, string currency = "USD");
}

public class AccountRepository : IAccountRepository
{
    private readonly IDbConnectionFactory _dbConnectionFactory;

    public AccountRepository(IDbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    public async Task<int> CreateAccountAsync(int userId, string currency = "USD", decimal initialBalance = 0.00m)
    {
        using var connection = _dbConnectionFactory.CreateConnection();

        var parameters = new DynamicParameters();
        parameters.Add("@UserId", userId, DbType.Int32);
        parameters.Add("@Currency", currency, DbType.String, size: 10);
        parameters.Add("@InitialBalance", initialBalance, DbType.Decimal);
        parameters.Add("@AccountId", dbType: DbType.Int32, direction: ParameterDirection.Output);

        await connection.ExecuteAsync(
            "usp_CreateAccount",
            parameters,
            commandType: CommandType.StoredProcedure
        );

        return parameters.Get<int>("@AccountId");
    }

    public async Task<Account?> GetAccountAsync(int userId, string currency = "USD")
    {
        using var connection = _dbConnectionFactory.CreateConnection();

        var parameters = new
        {
            UserId = userId,
            Currency = currency
        };

        return await connection.QueryFirstOrDefaultAsync<Account>(
            "usp_GetAccount",
            parameters,
            commandType: CommandType.StoredProcedure
        );
    }

    public async Task<decimal> GetAccountBalanceAsync(int userId, string currency = "USD")
    {
        using var connection = _dbConnectionFactory.CreateConnection();

        var parameters = new
        {
            UserId = userId,
            Currency = currency
        };

        return await connection.QueryFirstOrDefaultAsync<decimal>(
            "usp_GetAccountBalance",
            parameters,
            commandType: CommandType.StoredProcedure
        );
    }
}
