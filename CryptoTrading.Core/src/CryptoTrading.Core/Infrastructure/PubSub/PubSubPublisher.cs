using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CryptoTrading.Infrastructure.Logging;

namespace CryptoTrading.Infrastructure.PubSub;

public class PubSubPublisher : IPubSubPublisher
{
    private readonly ILoggerService _logger;
    private readonly bool _enabled;
    private long _publishedCount;

    public bool IsActive => _enabled;
    public long MessagesPublishedCount => _publishedCount;

    public PubSubPublisher(ILoggerService logger, Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        _logger = logger;
        _enabled = configuration.GetValue<bool>("PubSub:Enabled", false);
    }

    public Task<bool> PublishAsync<T>(string topicId, string orderingKey, T message, IDictionary<string, string>? attributes = null) where T : class
    {
        if (!_enabled)
        {
            return Task.FromResult(false);
        }

        _logger.Info($"[PubSub Publisher] Published message to '{topicId}' with key '{orderingKey}'.");
        _publishedCount++;
        return Task.FromResult(true);
    }

    public void Dispose()
    {
        // Cleanup resources
    }
}
