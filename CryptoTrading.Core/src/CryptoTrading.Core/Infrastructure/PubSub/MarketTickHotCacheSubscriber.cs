using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CryptoTrading.Infrastructure.Logging;
using CryptoTrading.Models.DTOs;

namespace CryptoTrading.Infrastructure.PubSub;

public class MarketTickHotCacheSubscriber : IDisposable
{
    private readonly IPubSubSubscriber? _subscriber;
    private readonly PubSubConfig _config;
    private readonly ILoggerService _logger;
    private static readonly ConcurrentDictionary<string, CryptocurrencyDto> _staticCache =
        new ConcurrentDictionary<string, CryptocurrencyDto>(StringComparer.OrdinalIgnoreCase);

    public int CachedTickCount => _staticCache.Count;

    public MarketTickHotCacheSubscriber(
        ILoggerService logger,
        Microsoft.Extensions.Configuration.IConfiguration configuration,
        IPubSubSubscriber? subscriber = null)
    {
        _subscriber = subscriber;
        _config = PubSubConfig.FromConfiguration(configuration);
        _logger = logger;
    }

    public void Start(CancellationToken ct = default)
    {
        if (_subscriber == null || !_config.Enabled)
        {
            _logger.Info("[MarketTickHotCacheSubscriber] GCP PubSub is disabled or subscriber is unconfigured. Hot Cache subscription bypassed.");
            return;
        }

        _logger.Info($"[MarketTickHotCacheSubscriber] Subscribing to market ticks on topic '{_config.MarketTicksTopic}'...");
        _subscriber.Subscribe<MarketTickEvent>(_config.MarketTicksTopic, HandleMarketTickAsync, ct);
    }

    private Task HandleMarketTickAsync(string symbolKey, MarketTickEvent tick)
    {
        if (tick == null || string.IsNullOrWhiteSpace(tick.Symbol))
            return Task.CompletedTask;

        var dto = new CryptocurrencyDto
        {
            CryptocurrencyId = tick.CryptoId,
            Symbol = tick.Symbol,
            Name = tick.Name,
            CurrentPrice = tick.CurrentPrice,
            PriceChange24h = tick.PriceChange24h,
            LastUpdated = tick.LastUpdated,
            IsActive = true
        };

        _staticCache.AddOrUpdate(tick.Symbol, dto, (k, old) => dto);
        _logger.Debug($"[MarketTickHotCacheSubscriber] Hot Cache updated: {tick.Symbol} = ${tick.CurrentPrice}");

        return Task.CompletedTask;
    }

    public static CryptocurrencyDto? GetLatestPrice(string symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol)) return null;
        _staticCache.TryGetValue(symbol.Trim(), out var dto);
        return dto;
    }

    public static void UpdateStaticPrice(string symbol, CryptocurrencyDto priceDto)
    {
        _staticCache.AddOrUpdate(symbol.Trim(), priceDto, (k, old) => priceDto);
    }

    public static IEnumerable<CryptocurrencyDto> GetAllPrices()
    {
        return _staticCache.Values.ToList();
    }

    public static void Clear()
    {
        _staticCache.Clear();
    }

    public void Dispose()
    {
        _staticCache.Clear();
    }
}
