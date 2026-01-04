using System.Threading;
using System.Threading.Tasks;

namespace Simple.Kafka.Consumer;

public interface IConsumerGroupManager
{
    Task SubscribeToEvents(CancellationToken cancellationToken);
    Task StartGroup(CancellationToken cancellationToken);
}
