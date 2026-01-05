using Simple.Kafka.Consumer.Primitives;

namespace Simple.Kafka.Consumer.Configuration;

internal sealed class RegisteredConsumerGroup(Group group)
{
    internal Group Group { get; } = group;
}
