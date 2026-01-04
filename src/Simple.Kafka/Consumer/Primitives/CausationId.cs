namespace Simple.Kafka.Consumer.Primitives;

public readonly record struct CausationId(string Topic, int Partition, long Offset)
{
    public override string ToString() => $"{Topic} [{Partition} @ {Offset}]";
    public static implicit operator string(CausationId causationId) => causationId.ToString();
}
