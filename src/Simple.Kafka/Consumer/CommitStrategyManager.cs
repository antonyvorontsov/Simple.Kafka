using System;
using System.Collections.Generic;
using System.Linq;
using Confluent.Kafka;
using Simple.Kafka.Consumer.Configuration;
using Simple.Kafka.Consumer.Primitives;

namespace Simple.Kafka.Consumer;

internal sealed class CommitStrategyManager : ICommitStrategyManager
{
    private readonly Dictionary<Group, Action<ConsumeResult<byte[], byte[]>>?> _groupCommitStrategies;

    public CommitStrategyManager(IEnumerable<RegisteredConsumerGroup> registeredGroups)
    {
        _groupCommitStrategies = registeredGroups.ToDictionary(x => x.Group, _ => (Action<ConsumeResult<byte[], byte[]>>?)null);
    }

    public void Set(Group group, Action<ConsumeResult<byte[], byte[]>> strategy)
    {
        if (!_groupCommitStrategies.ContainsKey(group))
        {
            throw new InvalidOperationException($"Could not set commit strategy, it has not been initialized for group {group} yet");
        }

        _groupCommitStrategies[group] = strategy;
    }

    public void Reset(Group group)
    {
        if (!_groupCommitStrategies.ContainsKey(group))
        {
            throw new InvalidOperationException($"Could not reset commit strategy, it has not been initialized for group {group} yet");
        }

        _groupCommitStrategies[group] = null;
    }

    public Action<ConsumeResult<byte[], byte[]>>? Get(Group group)
    {
        if (!_groupCommitStrategies.TryGetValue(group, out var strategy))
        {
            throw new InvalidOperationException($"Could not get commit strategy, it has not been initialized for group {group} yet");
        }

        return strategy;
    }
}
