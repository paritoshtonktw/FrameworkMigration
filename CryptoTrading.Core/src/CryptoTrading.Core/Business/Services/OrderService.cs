using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CryptoTrading.Data.Repositories;
using CryptoTrading.Infrastructure.Logging;
using CryptoTrading.Models.DTOs;

namespace CryptoTrading.Business.Services;

public interface IOrderService
{
    Task<IEnumerable<OrderDto>> GetOrdersAsync(int userId, int limit = 100);
    Task<OrderDto> GetOrderByIdAsync(int orderId, int userId);
    Task<OrderDto> CancelOrderAsync(int orderId, int userId);
}

public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepo;
    private readonly ILoggerService _logger;

    public OrderService(IOrderRepository orderRepo, ILoggerService logger)
    {
        _orderRepo = orderRepo;
        _logger = logger;
    }

    public async Task<IEnumerable<OrderDto>> GetOrdersAsync(int userId, int limit = 100)
    {
        return await _orderRepo.GetOrdersByUserAsync(userId, limit);
    }

    public async Task<OrderDto> GetOrderByIdAsync(int orderId, int userId)
    {
        var order = await _orderRepo.GetOrderAsync(orderId, userId);
        if (order == null)
        {
            throw new KeyNotFoundException($"Order #{orderId} was not found.");
        }
        return order;
    }

    public async Task<OrderDto> CancelOrderAsync(int orderId, int userId)
    {
        _logger.Info($"User #{userId} requested cancellation of order #{orderId}");
        var cancelledOrder = await _orderRepo.CancelOrderAsync(orderId, userId);
        if (cancelledOrder == null)
        {
            throw new KeyNotFoundException($"Order #{orderId} could not be found or cancelled for user #{userId}.");
        }
        _logger.Info($"Order #{orderId} successfully cancelled.");
        return cancelledOrder;
    }
}
