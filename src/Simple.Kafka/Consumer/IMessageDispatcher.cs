using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Simple.Kafka.Common;
using Simple.Kafka.Consumer.Primitives;

namespace Simple.Kafka.Consumer;

public interface IMessageDispatcher
{
    Task<ChannelState> Publish(Group group, ConsumeResult<byte[], byte[]> message, CancellationToken cancellationToken);
    // TODO: use it.
    Task PublishAndWait(Group group, ConsumeResult<byte[], byte[]> message, CancellationToken cancellationToken);
    Task SubscribeToMessages(CancellationToken cancellationToken);
    IAsyncEnumerable<Topic> ReadProcessedMessageTriggers(Group group, CancellationToken cancellationToken);
}
