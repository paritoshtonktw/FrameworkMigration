using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using CryptoTrading.Models.DTOs;

namespace CryptoTrading.Infrastructure.Sqs
{
    public class MarketTickHotCacheSubscriber
    {
        private static readonly ConcurrentDictionary<string, CryptocurrencyDto> _staticCache =
            new ConcurrentDictionary<string, CryptocurrencyDto>(StringComparer.OrdinalIgnoreCase);

        public static CryptocurrencyDto? GetLatestPrice(string symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol)) return null;
            _staticCache.TryGetValue(symbol.Trim(), out var dto);
            return dto;
        }

        public static void UpdatePrice(string symbol, CryptocurrencyDto dto)
        {
            _staticCache[symbol] = dto;
        }

        public static IEnumerable<CryptocurrencyDto> GetAllPrices()
        {
            return _staticCache.Values.ToList();
        }

        public static void Clear()
        {
            _staticCache.Clear();
        }
    }
}
