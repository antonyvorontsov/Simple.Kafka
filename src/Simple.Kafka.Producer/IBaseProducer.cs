using Confluent.Kafka;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Simple.Kafka.Producer;

/// <summary>
/// An internal producer which is responsible for sending
/// plain Confluent.Kafka messages.
/// </summary>
public interface IBaseProducer
{
    Task Produce(
        string topic,
        Message<byte[]?, byte[]?> message,
        CancellationToken cancellationToken);

    Task Produce(
        string topic,
        IReadOnlyCollection<Message<byte[]?, byte[]?>> messages,
        CancellationToken cancellationToken);
}