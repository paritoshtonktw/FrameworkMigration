using System;
using System.Threading;
using System.Threading.Tasks;

namespace CryptoTrading.Infrastructure.PubSub;

public interface IPubSubSubscriber : IDisposable
{
    void Subscribe<T>(string subscriptionId, Func<string, T, Task> handler, CancellationToken ct = default) where T : class;
    void Stop();
}
