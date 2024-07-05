using Confluent.Kafka;

namespace Simple.Kafka.Producer;

public interface IKafkaProducerHeadersEnricher
{
    Header? GetHeader();
}