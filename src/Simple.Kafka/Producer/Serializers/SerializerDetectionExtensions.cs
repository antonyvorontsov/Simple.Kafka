using System;
using System.Collections.Concurrent;
using System.Xml.Serialization;
using Google.Protobuf;
using Simple.Kafka.Producer.Serializers.Defaults;

namespace Simple.Kafka.Producer.Serializers;

internal static class SerializerDetectionExtensions
{
    private static readonly ConcurrentDictionary<Type, ISerializer> SerializersCache = new();

    internal static ISerializer GetDefaultSerializerOf<T>()
    {
        var messageType = typeof(T);
        return SerializersCache.GetOrAdd(
            messageType,
            // ReSharper disable once ConvertClosureToMethodGroup
            type => GetDefaultSerializerOfType(type));
    }

    private static ISerializer GetDefaultSerializerOfType(Type type)
    {
        if (type == typeof(byte[]))
        {
            return Default.Byte;
        }
        
        if (typeof(IMessage).IsAssignableFrom(type))
        {
            return Default.Proto;
        }

        if (Attribute.GetCustomAttribute(type, typeof(XmlRootAttribute)) != null)
        {
            return Default.Xml;
        }

        return Default.Json;
    }
    
    private static class Default
    {
        internal static readonly ISerializer Json = new DefaultJsonSerializer();
        internal static readonly ISerializer Proto = new DefaultProtobufSerializer();
        internal static readonly ISerializer Xml = new DefaultXmlSerializer();
        internal static readonly ISerializer Byte = new DefaultByteSerializer();
    }
}
