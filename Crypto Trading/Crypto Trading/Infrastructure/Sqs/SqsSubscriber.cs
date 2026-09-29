using System;
using System.Threading;
using System.Threading.Tasks;
using CryptoTrading.Infrastructure.Logging;
using Newtonsoft.Json;

namespace CryptoTrading.Infrastructure.Sqs
{
    /// <summary>
    /// AWS SQS Subscriber supporting in-memory event bus and polling/push models.
    /// </summary>
    public class SqsSubscriber : ISqsSubscriber
    {
        private readonly SqsConfig _config;
        private readonly ILoggerService _logger;
        private readonly SqsMessageHub _hub;
        private long _messagesReceivedCount = 0;
        private bool _isStopped = false;

        public bool IsActive => _config != null && _config.Enabled && !_isStopped;
        public long MessagesReceivedCount => Interlocked.Read(ref _messagesReceivedCount);

        public SqsSubscriber(SqsConfig config, ILoggerService logger)
        {
            _config = config ?? SqsConfig.FromConfiguration();
            _logger = logger;
            _hub = SqsMessageHub.Instance;
        }

        public void Subscribe<T>(string queueUrl, Func<string, T, Task> messageHandler, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(queueUrl))
                throw new ArgumentException("Queue URL cannot be empty", nameof(queueUrl));
            if (messageHandler == null)
                throw new ArgumentNullException(nameof(messageHandler));

            _logger?.Info($"[SqsSubscriber] Registering subscription for '{queueUrl}'...");

            _hub.Subscribe(queueUrl, async (messageGroupId, jsonPayload, attributes) =>
            {
                if (_isStopped || cancellationToken.IsCancellationRequested) return;

                try
                {
                    Interlocked.Increment(ref _messagesReceivedCount);
                    var deserialized = JsonConvert.DeserializeObject<T>(jsonPayload);
                    if (deserialized != null)
                    {
                        await messageHandler(messageGroupId, deserialized);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.Error($"[SqsSubscriber] Error processing message on '{queueUrl}' with message group ID '{messageGroupId}': {ex.Message}", ex);
                }
            });
        }

        public void Stop()
        {
            _isStopped = true;
            _logger?.Info("[SqsSubscriber] Subscriptions stopped.");
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
