using System;
using Confluent.Kafka;
using Simple.Kafka.Consumer.Primitives;

namespace Simple.Kafka.Consumer;

public interface ICommitStrategyManager
{
    Action<ConsumeResult<byte[], byte[]>>? Get(Group group);
    void Set(Group group, Action<ConsumeResult<byte[], byte[]>> strategy);
    void Reset(Group group);
}
