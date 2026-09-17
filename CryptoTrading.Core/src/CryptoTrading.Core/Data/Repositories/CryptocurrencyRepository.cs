using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using CryptoTrading.Data;
using CryptoTrading.Models.Entities;

namespace CryptoTrading.Data.Repositories;

public interface ICryptocurrencyRepository
{
    Task<IEnumerable<Cryptocurrency>> GetCryptocurrenciesAsync();
    Task<Cryptocurrency?> GetCryptocurrencyBySymbolAsync(string symbol);
    Task SaveCryptoPriceAsync(string symbol, decimal price, decimal priceChange24h);
    Task<Cryptocurrency?> GetLatestCryptoPriceAsync(string symbol);
    Task<IEnumerable<PriceHistory>> GetCryptoPriceHistoryAsync(string symbol, int limit = 100);
}

public class CryptocurrencyRepository : ICryptocurrencyRepository
{
    private readonly IDbConnectionFactory _dbConnectionFactory;

    public CryptocurrencyRepository(IDbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    public async Task<IEnumerable<Cryptocurrency>> GetCryptocurrenciesAsync()
    {
        using var connection = _dbConnectionFactory.CreateConnection();

        return await connection.QueryAsync<Cryptocurrency>(
            "usp_GetCryptocurrencies",
            commandType: CommandType.StoredProcedure
        );
    }

    public async Task<Cryptocurrency?> GetCryptocurrencyBySymbolAsync(string symbol)
    {
        using var connection = _dbConnectionFactory.CreateConnection();

        var parameters = new { Symbol = symbol };

        return await connection.QueryFirstOrDefaultAsync<Cryptocurrency>(
            "usp_GetCryptocurrencyBySymbol",
            parameters,
            commandType: CommandType.StoredProcedure
        );
    }

    public async Task SaveCryptoPriceAsync(string symbol, decimal price, decimal priceChange24h)
    {
        using var connection = _dbConnectionFactory.CreateConnection();

        var parameters = new
        {
            Symbol = symbol,
            Price = price,
            PriceChange24h = priceChange24h
        };

        await connection.ExecuteAsync(
            "usp_SaveCryptoPrice",
            parameters,
            commandType: CommandType.StoredProcedure
        );
    }

    public async Task<Cryptocurrency?> GetLatestCryptoPriceAsync(string symbol)
    {
        using var connection = _dbConnectionFactory.CreateConnection();

        var parameters = new { Symbol = symbol };

        return await connection.QueryFirstOrDefaultAsync<Cryptocurrency>(
            "usp_GetLatestCryptoPrice",
            parameters,
            commandType: CommandType.StoredProcedure
        );
    }

    public async Task<IEnumerable<PriceHistory>> GetCryptoPriceHistoryAsync(string symbol, int limit = 100)
    {
        using var connection = _dbConnectionFactory.CreateConnection();

        var parameters = new { Symbol = symbol, Limit = limit };

        return await connection.QueryAsync<PriceHistory>(
            "usp_GetCryptoPriceHistory",
            parameters,
            commandType: CommandType.StoredProcedure
        );
    }
}
