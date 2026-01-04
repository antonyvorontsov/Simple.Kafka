using System;
using Simple.Kafka.Consumer.Deserializers;

namespace Simple.Kafka.Consumer.Builders;

public sealed class KafkaConsumerGroupBuilder<TConsumer, TKey, TBody>
{
    internal IDeserializer<TKey>? KeyDeserializer { get; private set; }
    internal IDeserializer<TBody>? BodyDeserializer { get; private set; }
    internal bool SkipDeserializationErrorsOption { get; private set; } = true;
    internal TimeSpan? MessageProcessingTimeout { get; private set; }

    public KafkaConsumerGroupBuilder<TConsumer, TKey, TBody> SetKeyDeserializer(IDeserializer<TKey> deserializer)
    {
        KeyDeserializer = deserializer;
        return this;
    }

    public KafkaConsumerGroupBuilder<TConsumer, TKey, TBody> SetBodyDeserializer(IDeserializer<TBody> deserializer)
    {
        BodyDeserializer = deserializer;
        return this;
    }

    public KafkaConsumerGroupBuilder<TConsumer, TKey, TBody> SkipDeserializationErrors(bool value)
    {
        SkipDeserializationErrorsOption = value;
        return this;
    }

    public KafkaConsumerGroupBuilder<TConsumer, TKey, TBody> SetMessageProcessingTimeout(TimeSpan timeout)
    {
        MessageProcessingTimeout = timeout;
        return this;
    }
}

