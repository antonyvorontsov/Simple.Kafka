namespace Simple.Kafka.Consumer.Primitives;

public readonly record struct Group(string Value)
{
    public static implicit operator Group(string value) => new(value);
    public static implicit operator string(Group group) => group.Value;
    public override string ToString() => Value;
}
