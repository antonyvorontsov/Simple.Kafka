using Confluent.Kafka;

namespace Simple.Kafka.Consumer;

public interface IKafkaBuilderFactory
{
    ConsumerBuilder<byte[], byte[]> Create();
}
