using System;
using Confluent.Kafka;
using Google.Protobuf;
using Simple.Kafka.Producer.Exceptions;

namespace Simple.Kafka.Producer.Serializers.Defaults;

internal sealed class DefaultProtobufSerializer : ISerializer
{
    public byte[]? Serialize<T>(T value)
    {
        if (value is not IMessage data)
        {
            throw new SimpleKafkaProducerSerializationException(
                typeof(T),
                $"For proper proto serialization an object has to implement {nameof(IMessage)}");
        }

        if (typeof(T) == typeof(Null) || typeof(T) == typeof(Ignore))
        {
            return null;
        }

        try
        {
            return data.ToByteArray();
        }
        catch (Exception exception)
        {
            throw new SimpleKafkaProducerSerializationException(typeof(T), exception);
        }
    }
}
