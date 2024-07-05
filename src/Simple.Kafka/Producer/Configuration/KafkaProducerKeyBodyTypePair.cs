namespace Simple.Kafka.Producer.Configuration;

internal sealed class KafkaProducerKeyBodyTypePair : IEquatable<KafkaProducerKeyBodyTypePair>
{
    public Type KeyType { get; }
    public Type BodyType { get; }

    public KafkaProducerKeyBodyTypePair(Type keyType, Type bodyType)
    {
        KeyType = keyType;
        BodyType = bodyType;
    }

    public bool Equals(KafkaProducerKeyBodyTypePair? other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return KeyType == other.KeyType && BodyType == other.BodyType;
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is KafkaProducerKeyBodyTypePair other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(KeyType, BodyType);
    }

    public static bool operator ==(KafkaProducerKeyBodyTypePair? left, KafkaProducerKeyBodyTypePair? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(KafkaProducerKeyBodyTypePair? left, KafkaProducerKeyBodyTypePair? right)
    {
        return !Equals(left, right);
    }

    public static implicit operator KafkaProducerKeyBodyTypePair((Type KeyType, Type BodyType) pair)
    {
        return new KafkaProducerKeyBodyTypePair(pair.KeyType, pair.BodyType);
    }
}