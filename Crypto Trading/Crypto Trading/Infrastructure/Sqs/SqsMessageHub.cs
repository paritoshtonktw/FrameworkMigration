using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CryptoTrading.Infrastructure.Sqs
{
    /// <summary>
    /// In-memory SQS message hub for lightning-fast local development,
    /// automated unit/integration testing, and offline fallback.
    /// </summary>
    public class SqsMessageHub
    {
        private static readonly Lazy<SqsMessageHub> _instance = 
            new Lazy<SqsMessageHub>(() => new SqsMessageHub());

        public static SqsMessageHub Instance => _instance.Value;

        private readonly ConcurrentDictionary<string, List<Func<string, string, IDictionary<string, string>, Task>>> _subscriptions =
            new ConcurrentDictionary<string, List<Func<string, string, IDictionary<string, string>, Task>>>(StringComparer.OrdinalIgnoreCase);

        private SqsMessageHub() { }

        public async Task PublishAsync(string queueUrl, string messageGroupId, string jsonPayload, IDictionary<string, string> attributes = null)
        {
            if (string.IsNullOrWhiteSpace(queueUrl)) return;

            if (_subscriptions.TryGetValue(queueUrl, out var handlers))
            {
                List<Func<string, string, IDictionary<string, string>, Task>> snapshot;
                lock (handlers)
                {
                    snapshot = new List<Func<string, string, IDictionary<string, string>, Task>>(handlers);
                }

                foreach (var handler in snapshot)
                {
                    try
                    {
                        await handler(messageGroupId, jsonPayload, attributes ?? new Dictionary<string, string>());
                    }
                    catch
                    {
                        // Prevent subscriber failure from breaking the hub
                    }
                }
            }
        }

        public void Subscribe(string queueUrl, Func<string, string, IDictionary<string, string>, Task> handler)
        {
            if (string.IsNullOrWhiteSpace(queueUrl) || handler == null) return;

            _subscriptions.AddOrUpdate(
                queueUrl,
                key => new List<Func<string, string, IDictionary<string, string>, Task>> { handler },
                (key, existing) =>
                {
                    lock (existing)
                    {
                        existing.Add(handler);
                    }
                    return existing;
                });
        }

        public void Clear()
        {
            _subscriptions.Clear();
        }
    }
}
