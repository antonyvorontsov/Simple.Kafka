using System.Collections.Generic;
using Simple.Kafka.Consumer.Primitives;

namespace Simple.Kafka.Consumer.Configuration;

internal sealed class ConsumerGroupConfiguration(Group group, CommitStrategy commitStrategy)
{
    private readonly HashSet<Topic> _topics = new();
    
    internal Group Group { get; } = group;
    internal IReadOnlyCollection<Topic> Topics => _topics;
    internal CommitStrategy CommitStrategy { get; private set; } = commitStrategy;

    internal void AddTopic(Topic topic)
    {
        _topics.Add(topic);
    }

    internal void SetCommitStrategy(CommitStrategy commitStrategy)
    {
        CommitStrategy = commitStrategy;
    }
}
