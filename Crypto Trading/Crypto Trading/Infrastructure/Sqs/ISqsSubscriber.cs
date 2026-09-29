using System;
using System.Threading;
using System.Threading.Tasks;

namespace CryptoTrading.Infrastructure.Sqs
{
    public interface ISqsSubscriber : IDisposable
    {
        bool IsActive { get; }
        long MessagesReceivedCount { get; }
        void Subscribe<T>(string queueUrl, Func<string, T, Task> messageHandler, CancellationToken cancellationToken = default);
        void Stop();
    }
}
