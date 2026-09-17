using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using CryptoTrading.Data.Repositories;
using CryptoTrading.Infrastructure.Logging;
using CryptoTrading.Infrastructure.PubSub;
using CryptoTrading.Models.DTOs;

namespace CryptoTrading.Infrastructure.MarketData;

public interface ICryptoMarketService
{
    Task<List<CryptocurrencyDto>> GetMarketCryptocurrenciesAsync(bool forceRefresh = false);
    Task<CryptocurrencyDto?> GetCryptocurrencyPriceAsync(string symbol, bool forceRefresh = false);
    Task<CryptoChartDto> GetMarketChartAsync(string symbol, string timeframe);
    Task SyncMarketPricesAsync();
}

public class CoinGeckoMarketService : ICryptoMarketService
{
    private readonly HttpClient _httpClient;
    private readonly ICryptocurrencyRepository _cryptoRepo;
    private readonly IMemoryCache _cache;
    private readonly ILoggerService _logger;
    private readonly IPubSubPublisher? _pubSubPublisher;

    private const string CacheKeyAllCryptos = "CoinGecko_All_Cryptos";
    private readonly TimeSpan _cacheDuration = TimeSpan.FromSeconds(20);

    private static volatile List<CryptocurrencyDto>? _lastSuccessfulMarketPrices;

    private static readonly Dictionary<string, int> SymbolToCryptoIdMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "BTC", 1 },
        { "ETH", 2 },
        { "SOL", 3 },
        { "ADA", 4 },
        { "XRP", 5 }
    };

    private static readonly Dictionary<string, string> SymbolToIdMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "BTC", "bitcoin" },
        { "ETH", "ethereum" },
        { "SOL", "solana" },
        { "ADA", "cardano" },
        { "XRP", "ripple" }
    };

    private static readonly Dictionary<string, string> IdToSymbolMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "bitcoin", "BTC" },
        { "ethereum", "ETH" },
        { "solana", "SOL" },
        { "cardano", "ADA" },
        { "ripple", "XRP" }
    };

    public CoinGeckoMarketService(
        HttpClient httpClient,
        ICryptocurrencyRepository cryptoRepo,
        IMemoryCache cache,
        ILoggerService logger,
        IPubSubPublisher? pubSubPublisher = null)
    {
        _httpClient = httpClient;
        _cryptoRepo = cryptoRepo;
        _cache = cache;
        _logger = logger;
        _pubSubPublisher = pubSubPublisher;
    }

    public async Task<List<CryptocurrencyDto>> GetMarketCryptocurrenciesAsync(bool forceRefresh = false)
    {
        if (!forceRefresh && _cache.TryGetValue(CacheKeyAllCryptos, out List<CryptocurrencyDto>? cachedPrices))
        {
            return cachedPrices!;
        }

        try
        {
            var ids = string.Join(",", SymbolToIdMap.Values);
            var endpoint = $"coins/markets?vs_currency=usd&ids={ids}&order=market_cap_desc&sparkline=false";

            var response = await _httpClient.GetAsync(endpoint);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);

                var result = new List<CryptocurrencyDto>();
                foreach (var element in doc.RootElement.EnumerateArray())
                {
                    var id = element.GetProperty("id").GetString() ?? "";
                    if (IdToSymbolMap.TryGetValue(id, out var symbol))
                    {
                        var price = element.GetProperty("current_price").GetDecimal();
                        var change24h = element.GetProperty("price_change_percentage_24h").GetDecimal();
                        var name = element.GetProperty("name").GetString() ?? "";

                        // Persist price to SQL Server via stored procedure
                        try
                        {
                            await _cryptoRepo.SaveCryptoPriceAsync(symbol, price, change24h);
                        }
                        catch (Exception dbEx)
                        {
                            _logger.Warn($"Failed to persist price for {symbol}: {dbEx.Message}");
                        }

                        SymbolToCryptoIdMap.TryGetValue(symbol, out var cryptoId);

                        result.Add(new CryptocurrencyDto
                        {
                            CryptocurrencyId = cryptoId,
                            Symbol = symbol,
                            Name = name,
                            CurrentPrice = price,
                            PriceChange24h = change24h,
                            LastUpdated = DateTime.UtcNow,
                            IsActive = true
                        });
                    }
                }

                if (result.Any())
                {
                    _lastSuccessfulMarketPrices = result;
                    _cache.Set(CacheKeyAllCryptos, result, _cacheDuration);
                    _logger.Info($"Fetched and cached fresh market prices from CoinGecko.");

                    // Stream price ticks to GCP PubSub if publisher is active
                    if (_pubSubPublisher != null && _pubSubPublisher.IsActive)
                    {
                        try
                        {
                            foreach (var coin in result)
                            {
                                var tick = new MarketTickEvent
                                {
                                    CryptoId = coin.CryptocurrencyId,
                                    Symbol = coin.Symbol,
                                    Name = coin.Name,
                                    CurrentPrice = coin.CurrentPrice,
                                    PriceChange24h = coin.PriceChange24h,
                                    LastUpdated = DateTime.UtcNow
                                };
                                await _pubSubPublisher.PublishAsync("crypto-market-ticks", tick.Symbol, tick);
                            }
                        }
                        catch (Exception psEx)
                        {
                            _logger.Warn($"[PubSub] Could not publish market ticks: {psEx.Message}");
                        }
                    }

                    return result;
                }
            }
            else
            {
                _logger.Warn($"CoinGecko API returned HTTP {response.StatusCode}. Falling back to cached/persisted prices.");
            }
        }
        catch (Exception ex)
        {
            _logger.Error("Error connecting to CoinGecko API. Falling back to database cached prices.", ex);
        }

        // If we have recent in-memory prices, return them
        if (_lastSuccessfulMarketPrices != null && _lastSuccessfulMarketPrices.Count > 0)
        {
            _cache.Set(CacheKeyAllCryptos, _lastSuccessfulMarketPrices, _cacheDuration);
            return _lastSuccessfulMarketPrices;
        }

        // Fallback to persisted database prices
        return await GetFallbackPricesFromDbAsync();
    }

    public async Task<CryptocurrencyDto?> GetCryptocurrencyPriceAsync(string symbol, bool forceRefresh = false)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            throw new ArgumentNullException(nameof(symbol));

        var normalizedSymbol = symbol.Trim().ToUpperInvariant();

        // Fast path: Read from static hot cache
        if (!forceRefresh)
        {
            var hotPrice = MarketTickHotCacheSubscriber.GetLatestPrice(normalizedSymbol);
            if (hotPrice != null && hotPrice.CurrentPrice > 0)
            {
                return hotPrice;
            }
        }

        var all = await GetMarketCryptocurrenciesAsync(forceRefresh);
        var match = all.FirstOrDefault(c => c.Symbol.Equals(normalizedSymbol, StringComparison.OrdinalIgnoreCase));
        if (match != null)
        {
            return match;
        }

        // Fallback directly to DB
        var dbCrypto = await _cryptoRepo.GetCryptocurrencyBySymbolAsync(normalizedSymbol);
        if (dbCrypto != null)
        {
            return new CryptocurrencyDto
            {
                CryptocurrencyId = dbCrypto.CryptocurrencyId,
                Symbol = dbCrypto.Symbol,
                Name = dbCrypto.Name,
                CurrentPrice = dbCrypto.CurrentPrice,
                PriceChange24h = dbCrypto.PriceChange24h,
                LastUpdated = dbCrypto.LastUpdated,
                IsActive = dbCrypto.IsActive
            };
        }

        return null;
    }

    public async Task SyncMarketPricesAsync()
    {
        await GetMarketCryptocurrenciesAsync(forceRefresh: true);
    }

    public async Task<CryptoChartDto> GetMarketChartAsync(string symbol, string timeframe)
    {
        var normalizedSymbol = (symbol ?? "BTC").Trim().ToUpperInvariant();
        var tf = (timeframe ?? "24h").Trim().ToLowerInvariant();
        var cacheKey = $"CoinGecko_Chart_{normalizedSymbol}_{tf}";

        if (_cache.TryGetValue(cacheKey, out CryptoChartDto? cachedChart))
        {
            return cachedChart!;
        }

        string days = "1";
        switch (tf)
        {
            case "1h":
            case "24h":
            case "1d":
                days = "1";
                break;
            case "7d":
            case "1w":
                days = "7";
                break;
            case "1m":
            case "30d":
                days = "30";
                break;
            case "1y":
            case "365d":
                days = "365";
                break;
            case "all":
            case "max":
                days = "max";
                break;
            default:
                days = "1";
                break;
        }

        var chartDto = new CryptoChartDto
        {
            Symbol = normalizedSymbol,
            Timeframe = tf.ToUpperInvariant()
        };

        try
        {
            if (SymbolToIdMap.TryGetValue(normalizedSymbol, out var coinId))
            {
                var endpoint = $"coins/{coinId}/market_chart?vs_currency=usd&days={days}";
                var response = await _httpClient.GetAsync(endpoint);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);

                    if (doc.RootElement.TryGetProperty("prices", out var pricesArray) && pricesArray.ValueKind == JsonValueKind.Array)
                    {
                        var points = new List<PricePointDto>();
                        var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                        var oneHourAgoMs = nowMs - (60 * 60 * 1000);

                        foreach (var item in pricesArray.EnumerateArray())
                        {
                            var ts = item[0].GetInt64();
                            var price = item[1].GetDecimal();

                            if (tf == "1h" && ts < oneHourAgoMs)
                                continue;

                            points.Add(new PricePointDto
                            {
                                Timestamp = ts,
                                Date = DateTimeOffset.FromUnixTimeMilliseconds(ts).UtcDateTime,
                                Price = Math.Round(price, price < 1 ? 4 : 2)
                            });
                        }

                        if (points.Count > 0)
                        {
                            chartDto.Prices = points;
                            chartDto.CurrentPrice = points.Last().Price;
                            chartDto.HighPrice = points.Max(p => p.Price);
                            chartDto.LowPrice = points.Min(p => p.Price);

                            var firstPrice = points.First().Price;
                            chartDto.PriceChange = chartDto.CurrentPrice - firstPrice;
                            chartDto.PriceChangePercentage = firstPrice > 0 ? ((chartDto.PriceChange / firstPrice) * 100m) : 0m;

                            _cache.Set(cacheKey, chartDto, TimeSpan.FromMinutes(5));
                            return chartDto;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Warn($"CoinGecko chart fetch failed for {normalizedSymbol}: {ex.Message}");
        }

        // Fallback: build chart from current crypto price
        var crypto = await GetCryptocurrencyPriceAsync(normalizedSymbol);
        var basePrice = crypto?.CurrentPrice ?? (normalizedSymbol == "BTC" ? 64000m : normalizedSymbol == "ETH" ? 3400m : 100m);
        var change24h = crypto?.PriceChange24h ?? 1.5m;

        chartDto.CurrentPrice = basePrice;
        chartDto.Prices = GenerateFallbackPrices(basePrice, change24h, tf);
        chartDto.HighPrice = chartDto.Prices.Max(p => p.Price);
        chartDto.LowPrice = chartDto.Prices.Min(p => p.Price);
        var openP = chartDto.Prices.First().Price;
        chartDto.PriceChange = chartDto.CurrentPrice - openP;
        chartDto.PriceChangePercentage = openP > 0 ? ((chartDto.PriceChange / openP) * 100m) : 0m;

        _cache.Set(cacheKey, chartDto, TimeSpan.FromMinutes(2));
        return chartDto;
    }

    private static List<PricePointDto> GenerateFallbackPrices(decimal currentPrice, decimal change24h, string timeframe)
    {
        var list = new List<PricePointDto>();
        int pointCount = timeframe == "1h" ? 24 : timeframe == "7d" ? 28 : timeframe == "1m" ? 30 : timeframe == "1y" ? 52 : 36;
        TimeSpan step = timeframe == "1h" ? TimeSpan.FromMinutes(2.5) :
                        timeframe == "7d" ? TimeSpan.FromHours(6) :
                        timeframe == "1m" ? TimeSpan.FromDays(1) :
                        timeframe == "1y" ? TimeSpan.FromDays(7) :
                        timeframe == "all" ? TimeSpan.FromDays(30) :
                        TimeSpan.FromMinutes(40);

        var now = DateTimeOffset.UtcNow;
        var startPrice = currentPrice / (1m + (change24h / 100m));
        if (startPrice <= 0) startPrice = currentPrice * 0.95m;

        var random = new Random(currentPrice.GetHashCode());
        decimal currentWalk = startPrice;

        for (int i = 0; i < pointCount; i++)
        {
            var time = now - TimeSpan.FromTicks(step.Ticks * (pointCount - 1 - i));
            if (i == pointCount - 1)
            {
                currentWalk = currentPrice;
            }
            else
            {
                double progress = (double)i / (pointCount - 1);
                decimal trendTarget = startPrice + ((currentPrice - startPrice) * (decimal)progress);
                decimal noise = ((decimal)(random.NextDouble() - 0.48) * 0.03m * currentPrice);
                currentWalk = Math.Max(0.0001m, trendTarget + noise);
            }

            list.Add(new PricePointDto
            {
                Timestamp = time.ToUnixTimeMilliseconds(),
                Date = time.UtcDateTime,
                Price = Math.Round(currentWalk, currentWalk < 1 ? 4 : 2)
            });
        }

        return list;
    }

    private async Task<List<CryptocurrencyDto>> GetFallbackPricesFromDbAsync()
    {
        var dbCryptos = await _cryptoRepo.GetCryptocurrenciesAsync();
        return dbCryptos.Select(c => new CryptocurrencyDto
        {
            CryptocurrencyId = c.CryptocurrencyId,
            Symbol = c.Symbol,
            Name = c.Name,
            CurrentPrice = c.CurrentPrice,
            PriceChange24h = c.PriceChange24h,
            LastUpdated = c.LastUpdated,
            IsActive = c.IsActive
        }).ToList();
    }
}
