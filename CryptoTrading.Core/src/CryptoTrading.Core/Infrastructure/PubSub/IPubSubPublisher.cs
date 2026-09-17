using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CryptoTrading.Infrastructure.PubSub;

public interface IPubSubPublisher : IDisposable
{
    bool IsActive { get; }
    long MessagesPublishedCount { get; }
    Task<bool> PublishAsync<T>(string topicId, string orderingKey, T message, IDictionary<string, string>? attributes = null) where T : class;
}
