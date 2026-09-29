using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CryptoTrading.Infrastructure.Logging;
using CryptoTrading.Models.DTOs;

namespace CryptoTrading.Infrastructure.Sqs
{
    /// <summary>
    /// Subscribes to crypto-market-ticks in AWS SQS queue and maintains an in-memory hot cache
    /// for sub-millisecond price lookups without hitting CoinGecko API or SQL Server.
    /// </summary>
    public class MarketTickHotCacheSubscriber : IDisposable
    {
        private readonly ISqsSubscriber _subscriber;
        private readonly SqsConfig _config;
        private readonly ILoggerService _logger;
        private static readonly ConcurrentDictionary<string, CryptocurrencyDto> _staticCache =
            new ConcurrentDictionary<string, CryptocurrencyDto>(StringComparer.OrdinalIgnoreCase);

        public int CachedTickCount => _staticCache.Count;

        public MarketTickHotCacheSubscriber(
            ISqsSubscriber subscriber,
            SqsConfig config,
            ILoggerService logger)
        {
            _subscriber = subscriber;
            _config = config ?? SqsConfig.FromConfiguration();
            _logger = logger;
        }

        public void Start(CancellationToken ct = default)
        {
            _logger?.Info($"[MarketTickHotCacheSubscriber] Subscribing to market ticks on queue '{_config.MarketTicksQueueUrl}'...");
            _subscriber.Subscribe<MarketTickEvent>(_config.MarketTicksQueueUrl, HandleMarketTickAsync, ct);
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
            _logger?.Debug($"[MarketTickHotCacheSubscriber] Hot Cache updated: {tick.Symbol} = ${tick.CurrentPrice}");

            return Task.CompletedTask;
        }

        public static CryptocurrencyDto GetLatestPrice(string symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol)) return null;
            _staticCache.TryGetValue(symbol.Trim(), out var dto);
            return dto;
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
}
