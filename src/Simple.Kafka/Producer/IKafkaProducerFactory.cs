using Confluent.Kafka;

namespace Simple.Kafka.Producer;

public interface IKafkaProducerFactory
{
    IProducer<byte[]?, byte[]?> Create();
}