using System;

namespace Simple.Kafka.Consumer.Exceptions;

public sealed class CommitStrategyException(string message) : Exception(message);
