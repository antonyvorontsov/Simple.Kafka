using System.Collections.Generic;
using Simple.Kafka.Consumer.Primitives;

namespace Simple.Kafka.Consumer.Configuration;

#pragma warning disable CS8618
public sealed class DispatcherConfiguration
{
    internal Dictionary<Group, GroupTargetsConfiguration> GroupTargets { get; } = new();
}
