using System;
using System.Collections.Generic;
using System.Linq;
using System.Configuration;
using System.Net.Http;
using System.Runtime.Caching;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using CryptoTrading.Data.Repositories;
using CryptoTrading.Infrastructure.Logging;
using CryptoTrading.Infrastructure.Sqs;
using CryptoTrading.Models.DTOs;
using CryptoTrading.Models.Entities;

namespace CryptoTrading.Infrastructure.MarketData
{
    public interface ICryptoMarketService
    {
        Task<List<CryptocurrencyDto>> GetMarketCryptocurrenciesAsync(bool forceRefresh = false);
        Task<CryptocurrencyDto> GetCryptocurrencyPriceAsync(string symbol, bool forceRefresh = false);
        Task<CryptoChartDto> GetMarketChartAsync(string symbol, string timeframe);
        Task SyncMarketPricesAsync();
    }

    public class CoinGeckoMarketService : ICryptoMarketService
    {
        private readonly ICryptocurrencyRepository _cryptoRepo;
        private readonly ILoggerService _logger;
        private readonly ISqsPublisher _sqsPublisher;
        private static readonly HttpClient _httpClient = new HttpClient();
        private static readonly MemoryCache _cache = MemoryCache.Default;
        private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(
            int.TryParse(Environment.GetEnvironmentVariable("MARKET_REFRESH_INTERVAL_SECONDS") 
                ?? ConfigurationManager.AppSettings["CacheDurationSeconds"], out var sec) && sec > 0 ? sec : 20
        );
        private static volatile List<CryptocurrencyDto> _lastSuccessfulMarketPrices = null;

        private static readonly Dictionary<string, int> SymbolToCryptoIdMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            { "BTC", 1 },
            { "ETH", 2 },
            { "SOL", 3 },
            { "ADA", 4 },
            { "XRP", 5 }
        };

        private static readonly Dictionary<string, string> SymbolToIdMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "BTC", "bitcoin" },
            { "ETH", "ethereum" },
            { "SOL", "solana" },
            { "ADA", "cardano" },
            { "XRP", "ripple" }
        };

        private static readonly Dictionary<string, string> IdToSymbolMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "bitcoin", "BTC" },
            { "ethereum", "ETH" },
            { "solana", "SOL" },
            { "cardano", "ADA" },
            { "ripple", "XRP" }
        };

        static CoinGeckoMarketService()
        {
            _httpClient.BaseAddress = new Uri("https://api.coingecko.com/api/v3/");
            _httpClient.Timeout = TimeSpan.FromSeconds(10);
            if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
            {
                _httpClient.DefaultRequestHeaders.Add("User-Agent", "CryptoTradingPlatform/1.0 (.NETFramework4.7)");
                _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            }
        }

        public CoinGeckoMarketService(ICryptocurrencyRepository cryptoRepo, ILoggerService logger)
            : this(cryptoRepo, logger, null)
        {
        }

        public CoinGeckoMarketService(ICryptocurrencyRepository cryptoRepo, ILoggerService logger, ISqsPublisher sqsPublisher)
        {
            _cryptoRepo = cryptoRepo;
            _logger = logger;
            _sqsPublisher = sqsPublisher;
        }

        public async Task<List<CryptocurrencyDto>> GetMarketCryptocurrenciesAsync(bool forceRefresh = false)
        {
            const string cacheKey = "CoinGecko_All_Cryptos";

            if (!forceRefresh && _cache.Contains(cacheKey))
            {
                return (List<CryptocurrencyDto>)_cache.Get(cacheKey);
            }

            try
            {
                var ids = string.Join(",", SymbolToIdMap.Values);
                var endpoint = $"coins/markets?vs_currency=usd&ids={ids}&order=market_cap_desc&sparkline=false";

                var response = await _httpClient.GetAsync(endpoint);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var jArray = JArray.Parse(json);

                    var result = new List<CryptocurrencyDto>();
                    foreach (var item in jArray)
                    {
                        var id = (string)item["id"];
                        if (IdToSymbolMap.TryGetValue(id, out var symbol))
                        {
                            var price = (decimal)(item["current_price"] ?? 0m);
                            var change24h = (decimal)(item["price_change_percentage_24h"] ?? 0m);
                            var name = (string)item["name"];

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
                        _cache.Set(cacheKey, result, DateTimeOffset.UtcNow.Add(CacheDuration));
                        _logger.Info($"Fetched and cached fresh market prices for {result.Count} cryptocurrencies from CoinGecko (forceRefresh={forceRefresh}).");

                        // Stream price ticks to AWS SQS queue crypto-market-ticks
                        if (_sqsPublisher != null && _sqsPublisher.IsActive)
                        {
                            try
                            {
                                var sqsQueueUrl = SqsConfig.FromConfiguration().MarketTicksQueueUrl;
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
                                    await _sqsPublisher.PublishAsync(sqsQueueUrl, tick.Symbol, tick);
                                }
                            }
                            catch (Exception sqsEx)
                            {
                                _logger.Warn($"[SQS] Could not publish market ticks: {sqsEx.Message}");
                            }
                        }

                        return result;
                    }
                }
                else
                {
                    _logger.Warn($"CoinGecko API returned HTTP {response.StatusCode}. Falling back to database persisted prices.");
                    _logger.Warn($"CoinGecko API returned HTTP {response.StatusCode}. Falling back to last known cached or persisted prices.");
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Error connecting to CoinGecko API. Falling back to database cached prices.", ex);
            }

            // If we have recent in-memory prices, return them and re-cache briefly to absorb rate-limiting (e.g. 5s intervals)
            if (_lastSuccessfulMarketPrices != null && _lastSuccessfulMarketPrices.Count > 0)
            {
                _cache.Set(cacheKey, _lastSuccessfulMarketPrices, DateTimeOffset.UtcNow.Add(CacheDuration));
                return _lastSuccessfulMarketPrices;
            }

            // Fallback to persisted database prices (SRS Section 50)
            return await GetFallbackPricesFromDbAsync();
        }

        public async Task<CryptocurrencyDto> GetCryptocurrencyPriceAsync(string symbol, bool forceRefresh = false)
        {
            if (string.IsNullOrWhiteSpace(symbol))
                throw new ArgumentNullException(nameof(symbol));

            var normalizedSymbol = symbol.Trim().ToUpperInvariant();

            // 1. Fast path: Read from Pub/Sub in-memory hot cache (< 1ms)
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

            if (_cache.Contains(cacheKey))
            {
                return (CryptoChartDto)_cache.Get(cacheKey);
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
                        var jObj = JObject.Parse(json);
                        var pricesArray = jObj["prices"] as JArray;

                        if (pricesArray != null && pricesArray.Count > 0)
                        {
                            var points = new List<PricePointDto>();
                            var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                            var oneHourAgoMs = nowMs - (60 * 60 * 1000);

                            foreach (var item in pricesArray)
                            {
                                var ts = (long)item[0];
                                var price = (decimal)item[1];

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

                                _cache.Set(cacheKey, chartDto, DateTimeOffset.UtcNow.AddMinutes(5));
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

            _cache.Set(cacheKey, chartDto, DateTimeOffset.UtcNow.AddMinutes(2));
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
}

