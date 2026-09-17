using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CryptoTrading.Data.Repositories;
using CryptoTrading.Infrastructure.Logging;
using CryptoTrading.Infrastructure.MarketData;
using CryptoTrading.Models.DTOs;
using CryptoTrading.Models.Requests;

namespace CryptoTrading.Business.Services;

public interface ITradingService
{
    Task<TradeDto> BuyAsync(int userId, BuyTradeRequest request);
    Task<TradeDto> SellAsync(int userId, SellTradeRequest request);
    Task<IEnumerable<TradeDto>> GetUserTradesAsync(int userId, int limit = 100);
    Task<TradeDto> ClosePositionAsync(int userId, string symbol);
}

public class TradingService : ITradingService
{
    private readonly ITradingRepository _tradingRepo;
    private readonly ICryptoMarketService _marketService;
    private readonly ILoggerService _logger;
    private readonly IWalletRepository? _walletRepo;

    public TradingService(
        ITradingRepository tradingRepo,
        ICryptoMarketService marketService,
        ILoggerService logger,
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
            _logger.Error($"Failed to execute BUY for {symbol}: valid execution price could not be obtained.");
            throw new InvalidOperationException($"Cannot execute buy order: active market price for '{symbol}' is currently unavailable.");
        }

        decimal executionPrice = crypto.CurrentPrice;

        try
        {
            _logger.Info($"Executing BUY order for UserId: {userId}, Symbol: {symbol}, Quantity: {request.Quantity}, Price: {executionPrice}");
            var trade = await _tradingRepo.ExecuteBuyOrderAsync(userId, symbol, request.Quantity, executionPrice);
            if (trade == null)
            {
                throw new InvalidOperationException("Failed to execute buy order in repository.");
            }
            _logger.Info($"Successfully executed BUY trade #{trade.TradeId} for UserId: {userId}");
            return trade;
        }
        catch (Exception ex)
        {
            _logger.Error($"Error executing BUY order for user {userId} on {symbol}: {ex.Message}", ex);
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
            _logger.Error($"Failed to execute SELL for {symbol}: valid execution price could not be obtained.");
            throw new InvalidOperationException($"Cannot execute sell order: active market price for '{symbol}' is currently unavailable.");
        }

        decimal executionPrice = crypto.CurrentPrice;

        try
        {
            _logger.Info($"Executing SELL order for UserId: {userId}, Symbol: {symbol}, Quantity: {request.Quantity}, Price: {executionPrice}");
            var trade = await _tradingRepo.ExecuteSellOrderAsync(userId, symbol, request.Quantity, executionPrice);
            if (trade == null)
            {
                throw new InvalidOperationException("Failed to execute sell order in repository.");
            }
            _logger.Info($"Successfully executed SELL trade #{trade.TradeId} for UserId: {userId}, Realized P/L: ${trade.RealizedProfitLoss}");
            return trade;
        }
        catch (Exception ex)
        {
            _logger.Error($"Error executing SELL order for user {userId} on {symbol}: {ex.Message}", ex);
            throw;
        }
    }

    public async Task<IEnumerable<TradeDto>> GetUserTradesAsync(int userId, int limit = 100)
    {
        return await _tradingRepo.GetTradesByUserAsync(userId, limit);
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

        _logger.Info($"Closing full position of {quantity} {normalizedSymbol} for UserId: {userId}");

        return await SellAsync(userId, new SellTradeRequest
        {
            Symbol = normalizedSymbol,
            Quantity = quantity,
            OrderType = "MARKET"
        });
    }
}
