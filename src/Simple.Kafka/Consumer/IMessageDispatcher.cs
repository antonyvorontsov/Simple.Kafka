using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Simple.Kafka.Consumer.Primitives;

namespace Simple.Kafka.Consumer;

public interface IMessageDispatcher
{
    Task<ChannelState> Publish(Group group, ConsumeResult<byte[], byte[]> message, CancellationToken cancellationToken);
    Task StartSubscriptions(CancellationToken cancellationToken);
    IAsyncEnumerable<Topic> GetMessageProcessedTriggers(Group group, CancellationToken cancellationToken);
}
