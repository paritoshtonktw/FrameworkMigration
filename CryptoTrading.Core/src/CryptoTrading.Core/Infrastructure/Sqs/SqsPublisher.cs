using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CryptoTrading.Infrastructure.Sqs
{
    public class SqsPublisher : ISqsPublisher
    {
        public bool IsActive => false;
        public long MessagesPublishedCount => 0;

        public Task<bool> PublishAsync<T>(string queueUrl, string messageGroupId, T message, IDictionary<string, string>? attributes = null) where T : class
        {
            return Task.FromResult(false);
        }

        public void Dispose()
        {
        }
    }
}
