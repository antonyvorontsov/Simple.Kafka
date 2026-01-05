using System;

namespace Simple.Kafka.Consumer.Primitives;

public sealed record Percent
{
    private readonly int _value;

    public Percent(int value)
    {
        if (value is < 0 or > 100)
        {
            throw new ArgumentException("Percentage has to be in range between 0 and 100");
        }

        _value = value;
    }

    public static implicit operator double(Percent percent) => (double)percent._value / 100;
}