using System;

namespace Simple.Kafka.Producer.Configuration;

internal sealed record KafkaProducerKeyBodyTypePair(Type KeyType, Type BodyType)
{
    public static implicit operator KafkaProducerKeyBodyTypePair((Type KeyType, Type BodyType) pair)
    {
        return new KafkaProducerKeyBodyTypePair(pair.KeyType, pair.BodyType);
    }
}