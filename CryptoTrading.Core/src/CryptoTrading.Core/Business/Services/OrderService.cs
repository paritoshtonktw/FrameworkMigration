using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using CryptoTrading.Data.Repositories;
using CryptoTrading.Models.DTOs;

namespace CryptoTrading.Business.Services
{
    public interface IOrderService
    {
        Task<List<OrderDto>> GetOrdersAsync(int userId, int limit = 100);
        Task<OrderDto> GetOrderByIdAsync(int orderId, int userId);
        Task<OrderDto> CancelOrderAsync(int orderId, int userId);
    }

    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepo;
        private readonly ILogger<OrderService> _logger;

        public OrderService(IOrderRepository orderRepo, ILogger<OrderService> logger)
        {
            _orderRepo = orderRepo;
            _logger = logger;
        }

        public async Task<List<OrderDto>> GetOrdersAsync(int userId, int limit = 100)
        {
            var orders = await _orderRepo.GetOrdersByUserAsync(userId, limit);
            return orders.ToList();
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
            _logger.LogInformation("User #{UserId} requested cancellation of order #{OrderId}", userId, orderId);
            var cancelledOrder = await _orderRepo.CancelOrderAsync(orderId, userId);
            if (cancelledOrder == null)
            {
                throw new KeyNotFoundException($"Order #{orderId} not found or could not be cancelled.");
            }
            _logger.LogInformation("Order #{OrderId} successfully cancelled.", orderId);
            return cancelledOrder;
        }
    }
}
