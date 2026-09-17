using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CryptoTrading.Data.Repositories;
using CryptoTrading.Infrastructure.MarketData;
using CryptoTrading.Models.DTOs;
using CryptoTrading.Models.Entities;

namespace CryptoTrading.Business.Services;

public interface ICryptoService
{
    Task<IEnumerable<CryptocurrencyDto>> GetCryptocurrenciesAsync(bool forceRefresh = false);
    Task<CryptocurrencyDto?> GetCryptocurrencyBySymbolAsync(string symbol);
    Task<IEnumerable<PriceHistory>> GetPriceHistoryAsync(string symbol, int limit = 100);
    Task<CryptoChartDto> GetMarketChartAsync(string symbol, string timeframe);
}

public class CryptoService : ICryptoService
{
    private readonly ICryptoMarketService _marketService;
    private readonly ICryptocurrencyRepository _cryptoRepo;

    public CryptoService(ICryptoMarketService marketService, ICryptocurrencyRepository cryptoRepo)
    {
        _marketService = marketService;
        _cryptoRepo = cryptoRepo;
    }

    public async Task<IEnumerable<CryptocurrencyDto>> GetCryptocurrenciesAsync(bool forceRefresh = false)
    {
        return await _marketService.GetMarketCryptocurrenciesAsync(forceRefresh);
    }

    public async Task<CryptocurrencyDto?> GetCryptocurrencyBySymbolAsync(string symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            throw new ArgumentNullException(nameof(symbol));

        return await _marketService.GetCryptocurrencyPriceAsync(symbol);
    }

    public async Task<IEnumerable<PriceHistory>> GetPriceHistoryAsync(string symbol, int limit = 100)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            throw new ArgumentNullException(nameof(symbol));

        return await _cryptoRepo.GetCryptoPriceHistoryAsync(symbol.ToUpperInvariant(), limit);
    }

    public async Task<CryptoChartDto> GetMarketChartAsync(string symbol, string timeframe)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            throw new ArgumentNullException(nameof(symbol));

        return await _marketService.GetMarketChartAsync(symbol, timeframe);
    }
}
