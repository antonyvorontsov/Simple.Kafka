using Confluent.Kafka;

namespace Simple.Kafka.Producer;

public interface IKafkaHeaderEnricher
{
    Header? GetHeader();
}