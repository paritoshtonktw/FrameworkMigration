using System.Threading.Tasks;

namespace CryptoTrading.Infrastructure.Sqs
{
    public interface IOrderExecutionProcessor
    {
        Task<OrderExecutedEvent> ProcessOrderAsync(OrderPlacedEvent order);
    }
}
