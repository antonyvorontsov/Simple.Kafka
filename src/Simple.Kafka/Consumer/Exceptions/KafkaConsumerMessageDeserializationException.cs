using System;

namespace Simple.Kafka.Consumer.Exceptions;

public sealed class KafkaConsumerMessageDeserializationException : Exception
{
    public KafkaConsumerMessageDeserializationException(Type type, string message)
        : base($"Could not deserialize an instance of type {type}. Reason: {message}")
    {
    }

    public KafkaConsumerMessageDeserializationException(Type type, Exception exception)
        : base($"Could not deserialize an instance of type {type}. Reason in inner exception", exception)
    {
    }
}
