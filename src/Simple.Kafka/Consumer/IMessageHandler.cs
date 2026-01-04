using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Simple.Kafka.Consumer.Primitives;

namespace Simple.Kafka.Consumer;

public interface IMessageHandler
{
    ValueTask Handle(Group group, ConsumeResult<byte[], byte[]> message, CancellationToken cancellationToken);
}

public interface IMessageHandler<TKey, TBody> : IMessageHandler
{
    ValueTask Handle(Group group, ConsumedKafkaMessage<TKey, TBody> message, CancellationToken cancellationToken);
}
