using System;

namespace Simple.Kafka.Consumer.Deserializers;

public interface IDeserializer<out T>
{
    T Deserialize(ReadOnlySpan<byte> data);
}

