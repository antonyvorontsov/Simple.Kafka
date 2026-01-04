using Confluent.Kafka;

namespace Simple.Kafka.Consumer;

public class KafkaBuilderFactory(ConsumerConfig config) : IKafkaBuilderFactory
{
    public ConsumerBuilder<byte[], byte[]> Create()
    {
        return new ConsumerBuilder<byte[], byte[]>(config);
    }
}
