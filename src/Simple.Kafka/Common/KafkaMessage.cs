using Confluent.Kafka;

namespace Simple.Kafka.Common;

public sealed record KafkaMessage<TKey, TBody>(TKey Key, TBody Body, Headers? Headers = null);