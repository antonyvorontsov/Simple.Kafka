using Simple.Kafka.Common;
using Simple.Kafka.Consumer.Primitives;

namespace Simple.Kafka.Consumer;

public readonly record struct ConsumedKafkaMessage<TKey, TBody>(
    KafkaMessage<TKey, TBody> Message,
    CausationId CausationId);
