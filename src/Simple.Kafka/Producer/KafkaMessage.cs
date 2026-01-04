using Confluent.Kafka;

namespace Simple.Kafka.Producer;

public sealed record KafkaMessage<TKey, TBody>(TKey Key, TBody Body, Headers? Headers = null);