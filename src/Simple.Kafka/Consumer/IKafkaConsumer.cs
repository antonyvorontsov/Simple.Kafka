using System.Threading;
using System.Threading.Tasks;
using Simple.Kafka.Common;
using Simple.Kafka.Consumer.Primitives;

namespace Simple.Kafka.Consumer;

public interface IKafkaConsumer<TKey, TBody>
{
    Task Handle(KafkaMessage<TKey, TBody> message, CausationId causationId, CancellationToken cancellationToken);
}
