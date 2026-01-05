using System;
using Simple.Kafka.Consumer.Primitives;

#pragma warning disable CS8618

namespace Simple.Kafka.Consumer.Configuration;

public sealed class MessageHandlerConfiguration<TKey, TBody>
{
    internal Group Group { get; set; }
    internal Func<byte[], TKey> KeyDeserializationFunction { get; set; }
    internal Func<byte[], TBody> BodyDeserializationFunction { get; set; }
    internal bool SkipDeserializationErrors { get; set; }
    internal TimeSpan? MessageProcessingTimeout { get; set; }
}
