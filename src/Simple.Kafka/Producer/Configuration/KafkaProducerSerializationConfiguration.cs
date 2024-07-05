using Simple.Kafka.Producer.Serializers;

namespace Simple.Kafka.Producer.Configuration;

internal sealed class KafkaProducerSerializationConfiguration
{
    public ISerializer? KeySerializer { get; set; }
    public ISerializer? BodySerializer { get; set; }
}