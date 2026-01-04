using System.Threading;
using System.Threading.Tasks;

namespace Simple.Kafka.Consumer;

public interface IKafkaConsumer<TKey, TBody>
{
    ValueTask Handle(ConsumedKafkaMessage<TKey, TBody> message, CancellationToken cancellationToken);
}
