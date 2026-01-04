namespace Simple.Kafka.Consumer.Primitives;

public readonly record struct ChannelState(bool CloseToBeFull)
{
    public static implicit operator ChannelState(bool value) => new(value);
}
