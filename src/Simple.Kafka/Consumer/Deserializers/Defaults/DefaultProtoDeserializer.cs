using System;
using Confluent.Kafka;
using Google.Protobuf;
using Simple.Kafka.Consumer.Exceptions;

namespace Simple.Kafka.Consumer.Deserializers.Defaults;

internal sealed class DefaultProtoDeserializer<T> : IDeserializer<T> where T : IMessage<T>
{
    private readonly MessageParser<T> _deserializer;

    public DefaultProtoDeserializer()
    {
        var deserializerType = typeof(MessageParser<>).MakeGenericType(typeof(T));
        _deserializer = (MessageParser<T>?)Activator.CreateInstance(deserializerType, new Func<T>(Activator.CreateInstance<T>)) ??
                        throw new InvalidOperationException($"Couldn't create instance of {deserializerType.Name}");
    }

    public T Deserialize(ReadOnlySpan<byte> data)
    {
        if (!typeof(IMessage).IsAssignableFrom(typeof(T)))
        {
            throw new KafkaConsumerMessageDeserializationException(
                typeof(T),
                $"For proper proto deserialization an object has to implement {nameof(IMessage)}");
        }

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
            return _deserializer.ParseFrom(data);
        }
        catch (Exception exception)
        {
            throw new KafkaConsumerMessageDeserializationException(typeof(T), exception);
        }
    }
}
