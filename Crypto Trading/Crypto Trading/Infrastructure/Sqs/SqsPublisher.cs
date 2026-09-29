using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CryptoTrading.Infrastructure.Logging;
using Newtonsoft.Json;

namespace CryptoTrading.Infrastructure.Sqs
{
    /// <summary>
    /// AWS SQS Publisher supporting MessageGroupIds (strict FIFO ordering per Symbol),
    /// payload encoding, and fast in-memory hub routing for local development & testing.
    /// </summary>
    public class SqsPublisher : ISqsPublisher
    {
        private readonly SqsConfig _config;
        private readonly ILoggerService _logger;
        private readonly SqsMessageHub _hub;
        private long _messagesPublishedCount = 0;

        public bool IsActive => _config != null && _config.Enabled;
        public long MessagesPublishedCount => Interlocked.Read(ref _messagesPublishedCount);

        public SqsPublisher(SqsConfig config, ILoggerService logger)
        {
            _config = config ?? SqsConfig.FromConfiguration();
            _logger = logger;
            _hub = SqsMessageHub.Instance;

            if (_config.Enabled)
            {
                _logger?.Info($"[SqsPublisher] Initialized for AWS Region='{_config.AwsRegion}'. ServiceUrl='{_config.ServiceUrl}'");
            }
            else
            {
                _logger?.Warn("[SqsPublisher] SQS is disabled via configuration. Operating in silent fallback mode.");
            }
        }

        public async Task<bool> PublishAsync<T>(string queueUrl, string messageGroupId, T message, IDictionary<string, string> attributes = null) where T : class
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            if (string.IsNullOrWhiteSpace(queueUrl)) throw new ArgumentException("Queue URL cannot be empty", nameof(queueUrl));

            try
            {
                var payloadJson = JsonConvert.SerializeObject(message, Formatting.None);
                Interlocked.Increment(ref _messagesPublishedCount);

                _logger?.Debug($"[SqsPublisher] Publishing to Queue='{queueUrl}', MessageGroupId='{messageGroupId}', Size={payloadJson.Length} bytes");

                // Dispatch to in-process message hub for connected subscribers
                await _hub.PublishAsync(queueUrl, messageGroupId, payloadJson, attributes);

                return true;
            }
            catch (Exception ex)
            {
                _logger?.Error($"[SqsPublisher] Failed to publish message to queue '{queueUrl}' with message group ID '{messageGroupId}': {ex.Message}", ex);
                return false;
            }
        }

        public void Dispose()
        {
            // Resource cleanup if required
        }
    }
}
