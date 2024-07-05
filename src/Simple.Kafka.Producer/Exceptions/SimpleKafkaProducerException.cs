using System;
using Confluent.Kafka;

namespace Simple.Kafka.Producer.Exceptions;

public sealed class SimpleKafkaProducerException : Exception
{
    public SimpleKafkaProducerException(ErrorCode errorCode, string reason) : base($"{errorCode}: {reason}")
    {
    }
}