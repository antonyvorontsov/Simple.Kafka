using System;
using Confluent.Kafka;

namespace Simple.Kafka.Producer.Exceptions;

public sealed class SimpleKafkaProducerException(ErrorCode errorCode, string reason)
    : Exception($"{errorCode}: {reason}");