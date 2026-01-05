using System.Threading;
using System.Threading.Tasks;

namespace Simple.Kafka.Consumer;

/// <summary>
/// Manages a Kafka consumer group, including consumption, pausing, and resuming partitions.
/// </summary>
public interface IConsumerGroupManager
{
    /// <summary>
    /// Subscribes to events from the message dispatcher to resume paused partitions.
    /// </summary>
    Task SubscribeToEvents(CancellationToken cancellationToken);

    /// <summary>
    /// Starts the consumer group.
    /// </summary>
    Task StartGroup(CancellationToken cancellationToken);
}
