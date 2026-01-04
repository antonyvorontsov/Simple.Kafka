using System;
using System.IO;
using System.Text;
using Confluent.Kafka;
using Simple.Kafka.Producer.Exceptions;

namespace Simple.Kafka.Producer.Serializers.Defaults;

internal sealed class DefaultXmlSerializer : ISerializer
{
    public byte[]? Serialize<T>(T data)
    {
        if (typeof(T) == typeof(Null) || typeof(T) == typeof(Ignore))
        {
            return null;
        }

        try
        {
            var serializer = new System.Xml.Serialization.XmlSerializer(typeof(T));
            using var stringWriter = new StringWriter();
            serializer.Serialize(stringWriter, data);

            var xmlString = stringWriter.ToString();
            return Encoding.UTF8.GetBytes(xmlString);
        }
        catch (Exception exception)
        {
            throw new SimpleKafkaProducerSerializationException(typeof(T), exception);
        }
    }
}
