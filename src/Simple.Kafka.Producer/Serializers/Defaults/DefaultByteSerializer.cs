using Simple.Kafka.Producer.Exceptions;

namespace Simple.Kafka.Producer.Serializers.Defaults;

internal sealed class DefaultByteSerializer : ISerializer
{
    public byte[]? Serialize<T>(T value)
    {
        if (typeof(T) != typeof(byte[]))
        {
            throw new SimpleKafkaProducerSerializationException(
                typeof(T),
                "Byte serializer can only handle raw byte[] messages");
        }

        return value as byte[];
    }
}
