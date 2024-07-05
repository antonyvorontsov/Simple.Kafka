using System.Text.Json;
using Confluent.Kafka;
using Simple.Kafka.Producer.Exceptions;

namespace Simple.Kafka.Producer.Serializers.Defaults;

internal sealed class DefaultJsonSerializer : ISerializer
{
    public byte[]? Serialize<T>(T value)
    {
        if (typeof(T) == typeof(Null) || typeof(T) == typeof(Ignore))
        {
            return null;
        }

        try
        {
            return JsonSerializer.SerializeToUtf8Bytes(value);
        }
        catch (Exception exception)
        {
            throw new SimpleKafkaProducerSerializationException(typeof(T), exception);
        }
    }
}
