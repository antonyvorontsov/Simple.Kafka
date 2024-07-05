using Simple.Kafka.Producer.Serializers;

namespace Simple.Kafka.Producer.Configuration;

public sealed class KafkaProducerConfiguration
{
    internal Dictionary<KafkaProducerKeyBodyTypePair, KafkaProducerSerializationConfiguration> Serializers { get; set; } = new();

    internal KafkaProducerSerializationConfiguration? GetSerializationConfiguration<TKey, TBody>()
    {
        return Serializers.GetValueOrDefault((typeof(TKey), typeof(TBody)));
    }
}

public sealed class KafkaProducerConfiguration<TKey, TBody>
{
    public Type KeyType => typeof(TKey);
    public Type BodyType => typeof(TBody);
    public string Topic { get; set; }
    public ISerializer KeySerializer { get; set; }
    public ISerializer BodySerializer { get; set; }
}