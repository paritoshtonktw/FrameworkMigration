using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using CryptoTrading.Data;
using CryptoTrading.Models.DTOs;

namespace CryptoTrading.Data.Repositories;

public interface IOrderRepository
{
    Task<int> CreateOrderAsync(int userId, string symbol, string orderType, string side, decimal quantity, decimal price, string status = "Pending");
    Task<OrderDto?> GetOrderAsync(int orderId, int? userId = null);
    Task<IEnumerable<OrderDto>> GetOrdersByUserAsync(int userId, int limit = 100);
    Task<IEnumerable<OrderDto>> GetOpenOrdersAsync(int userId);
    Task<IEnumerable<OrderDto>> GetCompletedOrdersAsync(int userId, int limit = 100);
    Task UpdateOrderStatusAsync(int orderId, string status, DateTime? executedDate = null);
    Task<OrderDto?> CancelOrderAsync(int orderId, int userId);
}

public class OrderRepository : IOrderRepository
{
    private readonly IDbConnectionFactory _dbConnectionFactory;

    public OrderRepository(IDbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    public async Task<int> CreateOrderAsync(int userId, string symbol, string orderType, string side, decimal quantity, decimal price, string status = "Pending")
    {
        using var connection = _dbConnectionFactory.CreateConnection();

        var parameters = new DynamicParameters();
        parameters.Add("@UserId", userId, DbType.Int32);
        parameters.Add("@Symbol", symbol, DbType.String, size: 10);
        parameters.Add("@OrderType", orderType, DbType.String, size: 20);
        parameters.Add("@Side", side, DbType.String, size: 10);
        parameters.Add("@Quantity", quantity, DbType.Decimal);
        parameters.Add("@Price", price, DbType.Decimal);
        parameters.Add("@Status", status, DbType.String, size: 20);
        parameters.Add("@OrderId", dbType: DbType.Int32, direction: ParameterDirection.Output);

        await connection.ExecuteAsync(
            "usp_CreateOrder",
            parameters,
            commandType: CommandType.StoredProcedure
        );

        return parameters.Get<int>("@OrderId");
    }

    public async Task<OrderDto?> GetOrderAsync(int orderId, int? userId = null)
    {
        using var connection = _dbConnectionFactory.CreateConnection();

        var parameters = new { OrderId = orderId, UserId = userId };

        return await connection.QueryFirstOrDefaultAsync<OrderDto>(
            "usp_GetOrder",
            parameters,
            commandType: CommandType.StoredProcedure
        );
    }

    public async Task<IEnumerable<OrderDto>> GetOrdersByUserAsync(int userId, int limit = 100)
    {
        using var connection = _dbConnectionFactory.CreateConnection();

        var parameters = new { UserId = userId, Limit = limit };

        return await connection.QueryAsync<OrderDto>(
            "usp_GetOrdersByUser",
            parameters,
            commandType: CommandType.StoredProcedure
        );
    }

    public async Task<IEnumerable<OrderDto>> GetOpenOrdersAsync(int userId)
    {
        using var connection = _dbConnectionFactory.CreateConnection();

        var parameters = new { UserId = userId };

        return await connection.QueryAsync<OrderDto>(
            "usp_GetOpenOrders",
            parameters,
            commandType: CommandType.StoredProcedure
        );
    }

    public async Task<IEnumerable<OrderDto>> GetCompletedOrdersAsync(int userId, int limit = 100)
    {
        using var connection = _dbConnectionFactory.CreateConnection();

        var parameters = new { UserId = userId, Limit = limit };

        return await connection.QueryAsync<OrderDto>(
            "usp_GetCompletedOrders",
            parameters,
            commandType: CommandType.StoredProcedure
        );
    }

    public async Task UpdateOrderStatusAsync(int orderId, string status, DateTime? executedDate = null)
    {
        using var connection = _dbConnectionFactory.CreateConnection();

        var parameters = new { OrderId = orderId, Status = status, ExecutedDate = executedDate };

        await connection.ExecuteAsync(
            "usp_UpdateOrderStatus",
            parameters,
            commandType: CommandType.StoredProcedure
        );
    }

    public async Task<OrderDto?> CancelOrderAsync(int orderId, int userId)
    {
        using var connection = _dbConnectionFactory.CreateConnection();

        var parameters = new { OrderId = orderId, UserId = userId };

        return await connection.QueryFirstOrDefaultAsync<OrderDto>(
            "usp_CancelOrder",
            parameters,
            commandType: CommandType.StoredProcedure
        );
    }
}
