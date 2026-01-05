using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Confluent.Kafka;
using Simple.Kafka.Consumer.Exceptions;

namespace Simple.Kafka.Consumer.Deserializers.Defaults;

internal sealed class DefaultJsonDeserializer<T> : IDeserializer<T>
{
    private readonly JsonSerializerOptions _serializationOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };

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
            return JsonSerializer.Deserialize<T>(data, _serializationOptions)!;
        }
        catch (Exception exception)
        {
            throw new KafkaConsumerMessageDeserializationException(typeof(T), exception);
        }
    }
}
