using System;
using System.Collections.Generic;
using Simple.Kafka.Consumer.Primitives;

namespace Simple.Kafka.Consumer.Configuration;

public sealed class GroupTargetsConfiguration(Group group)
{
    private readonly Dictionary<Topic, Type> _topicTargets = new();

    public Group Group { get; } = group;
    public IReadOnlyDictionary<Topic, Type> TopicTargets => _topicTargets;

    public GroupTargetsConfiguration AddTarget(Topic topic, Type target)
    {
        _topicTargets[topic] = target;

        return this;
    }
}
