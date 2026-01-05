using System.Threading;
using System.Threading.Tasks;
using Simple.Kafka.Common;
using Simple.Kafka.Consumer.Primitives;

namespace Simple.Kafka.Consumer;

/// <summary>
/// Represents a Kafka consumer that processes messages of a specific key and body type.
/// This is the top-level consumer abstraction that is supposed to be implemented by a client.
/// </summary>
/// <typeparam name="TKey">The type of the message key.</typeparam>
/// <typeparam name="TBody">The type of the message body.</typeparam>
public interface IKafkaConsumer<TKey, TBody>
{
    /// <summary>
    /// Handles a deserialized Kafka message.
    /// </summary>
    /// <param name="message">The deserialized Kafka message.</param>
    /// <param name="causationId">The causation ID for tracing.</param>
    /// <param name="cancellationToken">A cancellation token to signal the termination of the operation.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous handling operation.</returns>
    Task Handle(KafkaMessage<TKey, TBody> message, CausationId causationId, CancellationToken cancellationToken);
}
