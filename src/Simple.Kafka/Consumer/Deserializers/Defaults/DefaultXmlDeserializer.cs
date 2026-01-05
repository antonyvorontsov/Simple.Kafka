using System;
using System.IO;
using System.Xml.Serialization;
using Confluent.Kafka;
using Simple.Kafka.Consumer.Exceptions;

namespace Simple.Kafka.Consumer.Deserializers.Defaults;

internal sealed class DefaultXmlDeserializer<T> : IDeserializer<T>
{
    public T Deserialize(ReadOnlySpan<byte> data)
    {
        if (typeof(T) == typeof(Null) || typeof(T) == typeof(Ignore))
        {
            return default!;
        }

        if (data.Length == 0)
        {
            return default!;
        }

        try
        {
            using var stream = new MemoryStream(data.ToArray());
            var serializer = new XmlSerializer(typeof(T));
            return (T)serializer.Deserialize(stream)!;
        }
        catch (Exception exception)
        {
            throw new KafkaConsumerMessageDeserializationException(typeof(T), exception);
        }
    }
}
