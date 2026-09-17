using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using CryptoTrading.Data;
using CryptoTrading.Models.DTOs;

namespace CryptoTrading.Data.Repositories;

public interface ITradingRepository
{
    Task<TradeDto?> ExecuteBuyOrderAsync(int userId, string symbol, decimal quantity, decimal executionPrice);
    Task<TradeDto?> ExecuteSellOrderAsync(int userId, string symbol, decimal quantity, decimal executionPrice);
    Task<IEnumerable<TradeDto>> GetTradesByUserAsync(int userId, int limit = 100);
}

public class TradingRepository : ITradingRepository
{
    private readonly IDbConnectionFactory _dbConnectionFactory;

    public TradingRepository(IDbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    public async Task<TradeDto?> ExecuteBuyOrderAsync(int userId, string symbol, decimal quantity, decimal executionPrice)
    {
        using var connection = _dbConnectionFactory.CreateConnection();

        var parameters = new DynamicParameters();
        parameters.Add("@UserId", userId, DbType.Int32);
        parameters.Add("@Symbol", symbol, DbType.String, size: 10);
        parameters.Add("@Quantity", quantity, DbType.Decimal);
        parameters.Add("@ExecutionPrice", executionPrice, DbType.Decimal);
        parameters.Add("@TradeId", dbType: DbType.Int32, direction: ParameterDirection.Output);

        return await connection.QueryFirstOrDefaultAsync<TradeDto>(
            "usp_ExecuteBuyOrder",
            parameters,
            commandType: CommandType.StoredProcedure
        );
    }

    public async Task<TradeDto?> ExecuteSellOrderAsync(int userId, string symbol, decimal quantity, decimal executionPrice)
    {
        using var connection = _dbConnectionFactory.CreateConnection();

        var parameters = new DynamicParameters();
        parameters.Add("@UserId", userId, DbType.Int32);
        parameters.Add("@Symbol", symbol, DbType.String, size: 10);
        parameters.Add("@Quantity", quantity, DbType.Decimal);
        parameters.Add("@ExecutionPrice", executionPrice, DbType.Decimal);
        parameters.Add("@TradeId", dbType: DbType.Int32, direction: ParameterDirection.Output);

        return await connection.QueryFirstOrDefaultAsync<TradeDto>(
            "usp_ExecuteSellOrder",
            parameters,
            commandType: CommandType.StoredProcedure
        );
    }

    public async Task<IEnumerable<TradeDto>> GetTradesByUserAsync(int userId, int limit = 100)
    {
        using var connection = _dbConnectionFactory.CreateConnection();

        var parameters = new { UserId = userId, Limit = limit };

        return await connection.QueryAsync<TradeDto>(
            "usp_GetTradesByUser",
            parameters,
            commandType: CommandType.StoredProcedure
        );
    }
}
