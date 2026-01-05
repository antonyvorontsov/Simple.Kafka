namespace Simple.Kafka.Common;

public readonly record struct Topic(string Value)
{
    public static implicit operator Topic(string value) => new(value);
    public static implicit operator string(Topic group) => group.Value;
    public override string ToString() => Value;
}
