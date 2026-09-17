using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using CryptoTrading.Data;
using CryptoTrading.Models.Entities;

namespace CryptoTrading.Data.Repositories;

public interface IWalletRepository
{
    Task<IEnumerable<Wallet>> GetWalletsAsync(int userId);
    Task<Wallet?> GetWalletHoldingAsync(int userId, string currency);
    Task<int> CreateWalletAsync(int userId, string currency, decimal initialQuantity = 0, decimal averageCost = 0);
    Task UpdateWalletHoldingAsync(int userId, string currency, decimal quantity, decimal averageCost);
}

public class WalletRepository : IWalletRepository
{
    private readonly IDbConnectionFactory _dbConnectionFactory;

    public WalletRepository(IDbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    public async Task<IEnumerable<Wallet>> GetWalletsAsync(int userId)
    {
        using var connection = _dbConnectionFactory.CreateConnection();

        var parameters = new { UserId = userId };

        return await connection.QueryAsync<Wallet>(
            "usp_GetWallets",
            parameters,
            commandType: CommandType.StoredProcedure
        );
    }

    public async Task<Wallet?> GetWalletHoldingAsync(int userId, string currency)
    {
        using var connection = _dbConnectionFactory.CreateConnection();

        var parameters = new { UserId = userId, Currency = currency };

        return await connection.QueryFirstOrDefaultAsync<Wallet>(
            "usp_GetWalletHolding",
            parameters,
            commandType: CommandType.StoredProcedure
        );
    }

    public async Task<int> CreateWalletAsync(int userId, string currency, decimal initialQuantity = 0, decimal averageCost = 0)
    {
        using var connection = _dbConnectionFactory.CreateConnection();

        var parameters = new DynamicParameters();
        parameters.Add("@UserId", userId, DbType.Int32);
        parameters.Add("@Currency", currency, DbType.String, size: 10);
        parameters.Add("@InitialQuantity", initialQuantity, DbType.Decimal);
        parameters.Add("@AverageCost", averageCost, DbType.Decimal);
        parameters.Add("@WalletId", dbType: DbType.Int32, direction: ParameterDirection.Output);

        await connection.ExecuteAsync(
            "usp_CreateWallet",
            parameters,
            commandType: CommandType.StoredProcedure
        );

        return parameters.Get<int>("@WalletId");
    }

    public async Task UpdateWalletHoldingAsync(int userId, string currency, decimal quantity, decimal averageCost)
    {
        using var connection = _dbConnectionFactory.CreateConnection();

        var parameters = new
        {
            UserId = userId,
            Currency = currency,
            Quantity = quantity,
            AverageCost = averageCost
        };

        await connection.ExecuteAsync(
            "usp_UpdateWalletHolding",
            parameters,
            commandType: CommandType.StoredProcedure
        );
    }
}
