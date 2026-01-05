using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Simple.Kafka.Common;
using Simple.Kafka.Consumer.Primitives;

namespace Simple.Kafka.Consumer;

public interface IMessageHandler
{
    Task Handle(Group group, ConsumeResult<byte[], byte[]> message, CancellationToken cancellationToken);
}

public interface IMessageHandler<TKey, TBody> : IMessageHandler
{
    Task Handle(
        Group group,
        KafkaMessage<TKey, TBody> message,
        CausationId causationId,
        CancellationToken cancellationToken);
}
