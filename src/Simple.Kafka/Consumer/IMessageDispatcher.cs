using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Simple.Kafka.Common;
using Simple.Kafka.Consumer.Primitives;

namespace Simple.Kafka.Consumer;

/// <summary>
/// Dispatches Kafka messages to appropriate handlers.
/// </summary>
public interface IMessageDispatcher
{
    /// <summary>
    /// Publishes a single message to the dispatcher.
    /// </summary>
    /// <param name="group">The consumer group the message belongs to.</param>
    /// <param name="message">The consumed Kafka message.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="ChannelState"/> indicating the state of the channel after publishing.</returns>
    Task<ChannelState> Publish(Group group, ConsumeResult<byte[], byte[]> message, CancellationToken cancellationToken);
    
    /// <summary>
    /// Publishes a single message to the dispatcher and waits for it to be processed.
    /// </summary>
    /// <param name="group">The consumer group the message belongs to.</param>
    /// <param name="message">The consumed Kafka message.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task PublishAndWait(Group group, ConsumeResult<byte[], byte[]> message, CancellationToken cancellationToken);
    
    /// <summary>
    /// Starts the subscriptions for message processing.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task SubscribeToMessages(CancellationToken cancellationToken);
    
    /// <summary>
    /// Reads triggers for processed messages for a specific consumer group.
    /// </summary>
    /// <param name="group">The consumer group.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An asynchronous enumerable of topics for which messages have been processed.</returns>
    IAsyncEnumerable<Topic> ReadProcessedMessageTriggers(Group group, CancellationToken cancellationToken);
}
