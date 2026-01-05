using System;
using System.Xml.Serialization;
using Google.Protobuf;
using Simple.Kafka.Consumer.Deserializers.Defaults;

namespace Simple.Kafka.Consumer.Deserializers;

internal static class DeserializerDetectionExtensions
{
    internal static IDeserializer<T> GetDefaultDeserializerOf<T>()
    {
        var type = typeof(T);
        if (typeof(IMessage).IsAssignableFrom(type))
        {
            return (IDeserializer<T>)Activator.CreateInstance(typeof(DefaultProtoDeserializer<>).MakeGenericType(typeof(T)))!;
        }

        if (Attribute.GetCustomAttribute(type, typeof(XmlRootAttribute)) != null)
        {
            return new DefaultXmlDeserializer<T>();
        }

        return new DefaultJsonDeserializer<T>();
    }
}
