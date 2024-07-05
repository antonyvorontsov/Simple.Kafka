using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Simple.Kafka.Producer;

public interface IKafkaProducer
{
    Task Produce<TKey, TBody>(
        string topic,
        TKey key,
        TBody body, 
        CancellationToken cancellationToken);

    Task Produce<TKey, TBody>(
        string topic,
        IReadOnlyCollection<KafkaMessage<TKey, TBody>> kafkaMessages,
        CancellationToken cancellationToken);
}

public interface IKafkaProducer<TKey, TBody>
{
    Task Produce(
        TKey key,
        TBody body,
        CancellationToken cancellationToken);

    Task Produce(
        IReadOnlyCollection<KafkaMessage<TKey, TBody>> kafkaMessages,
        CancellationToken cancellationToken);
}