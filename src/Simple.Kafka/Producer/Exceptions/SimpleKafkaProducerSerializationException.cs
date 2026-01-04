using System;

namespace Simple.Kafka.Producer.Exceptions;

public sealed class SimpleKafkaProducerSerializationException : Exception
{
    public SimpleKafkaProducerSerializationException(Type type, string message)
        : base($"Could not serialize a message of type {type}. Reason: {message}")
    {
    }

    public SimpleKafkaProducerSerializationException(Type type, Exception exception)
        : base($"Could not serialize a message of type {type}. Reason", exception)
    {
    }
}