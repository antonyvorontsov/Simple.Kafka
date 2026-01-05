using Confluent.Kafka;

namespace Simple.Kafka.Consumer.Primitives;

public sealed record CausationId(TopicPartitionOffset TopicPartitionOffset)
{
    public override string ToString() =>
        $"{TopicPartitionOffset.Topic} [{TopicPartitionOffset.Partition} @ {TopicPartitionOffset.Offset}]";

    public static implicit operator string(CausationId causationId) => causationId.ToString();
}
