using System;
using System.Collections.Generic;
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
    public required string Topic { get; set; }
    public required ISerializer KeySerializer { get; set; }
    public required ISerializer BodySerializer { get; set; }
}