using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CryptoTrading.Infrastructure.Sqs
{
    public interface ISqsPublisher : IDisposable
    {
        bool IsActive { get; }
        long MessagesPublishedCount { get; }
        Task<bool> PublishAsync<T>(string queueUrl, string messageGroupId, T message, IDictionary<string, string>? attributes = null) where T : class;
    }
}
