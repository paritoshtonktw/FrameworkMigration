using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using CryptoTrading.Data.Repositories;
using CryptoTrading.Infrastructure.MarketData;
using CryptoTrading.Models.DTOs;
using CryptoTrading.Models.Requests;

namespace CryptoTrading.Business.Services
{
    public interface ITradingService
    {
        Task<TradeDto> BuyAsync(int userId, BuyTradeRequest request);
        Task<TradeDto> SellAsync(int userId, SellTradeRequest request);
        Task<List<TradeDto>> GetUserTradesAsync(int userId, int limit = 100);
        Task<TradeDto> ClosePositionAsync(int userId, string symbol);
    }

    public class TradingService : ITradingService
    {
        private readonly ITradingRepository _tradingRepo;
        private readonly ICryptoMarketService _marketService;
        private readonly ILogger<TradingService> _logger;
        private readonly IWalletRepository? _walletRepo;

        public TradingService(
            ITradingRepository tradingRepo,
            ICryptoMarketService marketService,
            ILogger<TradingService> logger,
            IWalletRepository? walletRepo = null)
        {
            _tradingRepo = tradingRepo;
            _marketService = marketService;
            _logger = logger;
            _walletRepo = walletRepo;
        }

        public async Task<TradeDto> BuyAsync(int userId, BuyTradeRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (request.Quantity <= 0)
                throw new ArgumentException("Quantity must be greater than zero.", nameof(request.Quantity));

            var symbol = request.Symbol.Trim().ToUpperInvariant();

            // 1. Obtain current market price
            var crypto = await _marketService.GetCryptocurrencyPriceAsync(symbol);
            if (crypto == null || crypto.CurrentPrice <= 0)
            {
                _logger.LogError("Failed to execute BUY for {Symbol}: valid execution price could not be obtained.", symbol);
                throw new InvalidOperationException($"Cannot execute buy order: active market price for '{symbol}' is currently unavailable.");
            }

            decimal executionPrice = crypto.CurrentPrice;

            try
            {
                _logger.LogInformation("Executing BUY order for UserId: {UserId}, Symbol: {Symbol}, Quantity: {Quantity}, Price: {Price}", userId, symbol, request.Quantity, executionPrice);
                var trade = await _tradingRepo.ExecuteBuyOrderAsync(userId, symbol, request.Quantity, executionPrice);
                if (trade == null)
                {
                    throw new InvalidOperationException("Failed to register buy trade match execution.");
                }
                _logger.LogInformation("Successfully executed BUY trade #{TradeId} for UserId: {UserId}", trade.TradeId, userId);
                return trade;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing BUY order for user {UserId} on {Symbol}: {Message}", userId, symbol, ex.Message);
                throw;
            }
        }

        public async Task<TradeDto> SellAsync(int userId, SellTradeRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (request.Quantity <= 0)
                throw new ArgumentException("Quantity must be greater than zero.", nameof(request.Quantity));

            var symbol = request.Symbol.Trim().ToUpperInvariant();

            // 1. Obtain current market price
            var crypto = await _marketService.GetCryptocurrencyPriceAsync(symbol);
            if (crypto == null || crypto.CurrentPrice <= 0)
            {
                _logger.LogError("Failed to execute SELL for {Symbol}: valid execution price could not be obtained.", symbol);
                throw new InvalidOperationException($"Cannot execute sell order: active market price for '{symbol}' is currently unavailable.");
            }

            decimal executionPrice = crypto.CurrentPrice;

            try
            {
                _logger.LogInformation("Executing SELL order for UserId: {UserId}, Symbol: {Symbol}, Quantity: {Quantity}, Price: {Price}", userId, symbol, request.Quantity, executionPrice);
                var trade = await _tradingRepo.ExecuteSellOrderAsync(userId, symbol, request.Quantity, executionPrice);
                if (trade == null)
                {
                    throw new InvalidOperationException("Failed to register sell trade match execution.");
                }
                _logger.LogInformation("Successfully executed SELL trade #{TradeId} for UserId: {UserId}, Realized P/L: ${RealizedProfitLoss}", trade.TradeId, userId, trade.RealizedProfitLoss);
                return trade;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing SELL order for user {UserId} on {Symbol}: {Message}", userId, symbol, ex.Message);
                throw;
            }
        }

        public async Task<List<TradeDto>> GetUserTradesAsync(int userId, int limit = 100)
        {
            var trades = await _tradingRepo.GetTradesByUserAsync(userId, limit);
            return trades.ToList();
        }

        public async Task<TradeDto> ClosePositionAsync(int userId, string symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol))
                throw new ArgumentNullException(nameof(symbol));

            var normalizedSymbol = symbol.Trim().ToUpperInvariant();

            decimal quantity = 0m;
            if (_walletRepo != null)
            {
                var wallet = await _walletRepo.GetWalletHoldingAsync(userId, normalizedSymbol);
                if (wallet != null)
                {
                    quantity = wallet.Quantity;
                }
            }

            if (quantity <= 0)
            {
                throw new InvalidOperationException($"No open position or quantity available to close for {normalizedSymbol}.");
            }

            _logger.LogInformation("Closing full position of {Quantity} {Symbol} for UserId: {UserId}", quantity, normalizedSymbol, userId);

            return await SellAsync(userId, new SellTradeRequest
            {
                Symbol = normalizedSymbol,
                Quantity = quantity,
                OrderType = "MARKET"
            });
        }
    }
}
