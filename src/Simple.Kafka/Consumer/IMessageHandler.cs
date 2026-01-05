using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Simple.Kafka.Common;
using Simple.Kafka.Consumer.Primitives;

namespace Simple.Kafka.Consumer;

/// <summary>
/// Represents a message handler that processes raw Kafka messages.
/// </summary>
public interface IMessageHandler
{
    /// <summary>
    /// Handles a raw Kafka message.
    /// </summary>
    /// <param name="group">The consumer group the message belongs to.</param>
    /// <param name="message">The raw Kafka message consumed from the topic.</param>
    /// <param name="cancellationToken">A cancellation token to signal the termination of the operation.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous handling operation.</returns>
    Task Handle(Group group, ConsumeResult<byte[], byte[]> message, CancellationToken cancellationToken);
}

/// <summary>
/// Represents a message handler that processes deserialized Kafka messages.
/// </summary>
/// <typeparam name="TKey">The type of the message key.</typeparam>
/// <typeparam name="TBody">The type of the message body.</typeparam>
public interface IMessageHandler<TKey, TBody> : IMessageHandler
{
    /// <summary>
    /// Handles a deserialized Kafka message.
    /// </summary>
    /// <param name="message">The deserialized Kafka message.</param>
    /// <param name="causationId">The causation ID for tracing.</param>
    /// <param name="cancellationToken">A cancellation token to signal the termination of the operation.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous handling operation.</returns>
    Task Handle(
        KafkaMessage<TKey, TBody> message,
        CausationId causationId,
        CancellationToken cancellationToken);
}
