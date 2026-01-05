using System;

namespace Simple.Kafka.Consumer.Deserializers;

/// <summary>
/// Represents a deserializer that converts a read-only span of bytes into an object of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The type of the object to deserialize to.</typeparam>
public interface IDeserializer<out T>
{
    /// <summary>
    /// Deserializes a read-only span of bytes into an object of type <typeparamref name="T"/>.
    /// </summary>
    /// <param name="data">The read-only span of bytes to deserialize.</param>
    /// <returns>The deserialized object of type <typeparamref name="T"/>.</returns>
    T Deserialize(ReadOnlySpan<byte> data);
}
