using System;
using System.Threading;
using CryptoTrading.Business.Services;
using CryptoTrading.Infrastructure.Logging;

namespace CryptoTrading.Infrastructure.Sqs
{
    /// <summary>
    /// Lifecycle coordinator for AWS SQS publisher, subscribers, and background hot cache workers.
    /// </summary>
    public class SqsManager : IDisposable
    {
        private readonly SqsConfig _config;
        private readonly ILoggerService _logger;
        private readonly ISqsPublisher _publisher;
        private readonly ISqsSubscriber _subscriber;
        private readonly MarketTickHotCacheSubscriber _tickSubscriber;
        private readonly IOrderExecutionProcessor _orderProcessor;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private bool _isRunning = false;

        public ISqsPublisher Publisher => _publisher;
        public ISqsSubscriber Subscriber => _subscriber;
        public MarketTickHotCacheSubscriber TickSubscriber => _tickSubscriber;
        public IOrderExecutionProcessor OrderProcessor => _orderProcessor;
        public bool IsRunning => _isRunning;

        public SqsManager(
            SqsConfig config,
            ILoggerService logger,
            Func<ITradingService> tradingServiceFactory,
            IOrderExecutionProcessor orderProcessor = null)
        {
            _config = config ?? SqsConfig.FromConfiguration();
            _logger = logger;

            _publisher = new SqsPublisher(_config, _logger);
            _subscriber = new SqsSubscriber(_config, _logger);
            _orderProcessor = orderProcessor ?? new OrderExecutionProcessor(tradingServiceFactory, _publisher, _config, _logger);
            _tickSubscriber = new MarketTickHotCacheSubscriber(_subscriber, _config, _logger);
        }

        public void Start()
        {
            if (_isRunning) return;

            _logger?.Info("[SqsManager] Initializing AWS SQS streaming services...");

            if (_config.Enabled && _config.TickSubscriberEnabled)
            {
                _tickSubscriber.Start(_cts.Token);
                _logger?.Info("[SqsManager] MarketTickHotCacheSubscriber started (real-time price streaming).");
            }
            else
            {
                _logger?.Info("[SqsManager] MarketTickHotCacheSubscriber disabled.");
            }

            _isRunning = true;
            _logger?.Info("[SqsManager] AWS SQS event messaging is active.");
        }

        public void Stop()
        {
            if (!_isRunning) return;

            _logger?.Info("[SqsManager] Stopping AWS SQS services...");
            _cts.Cancel();
            _subscriber?.Stop();
            _isRunning = false;
        }

        public void Dispose()
        {
            Stop();
            _publisher?.Dispose();
            _subscriber?.Dispose();
            _tickSubscriber?.Dispose();
            _cts?.Dispose();
        }
    }
}
