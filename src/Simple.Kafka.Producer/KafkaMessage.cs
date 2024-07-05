namespace Simple.Kafka.Producer;

public readonly record struct KafkaMessage<TKey, TBody>(TKey Key, TBody Body);