using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Options;
using Simple.Kafka.Common;
using Simple.Kafka.Consumer.Configuration;
using Simple.Kafka.Consumer.Primitives;

namespace Simple.Kafka.Consumer;

internal sealed class MessageDispatcher : IMessageDispatcher
{
    private readonly Dictionary<Group, Dictionary<Topic, HandlerChannelContainer>> _messageHandlersMap;
    private readonly Dictionary<Group, Channel<Topic>> _messageProcessedTriggerChannelMap;

    public MessageDispatcher(
        IEnumerable<IMessageHandler> messageHandlers,
        IOptions<DispatcherConfiguration> dispatcherConfiguration)
    {
        _messageHandlersMap = ConstructMessageHandlersMap(messageHandlers, dispatcherConfiguration.Value.GroupTargets);
        _messageProcessedTriggerChannelMap = ConstructMessageProcessedTriggerChannelMap(dispatcherConfiguration.Value.GroupTargets);
    }

    private static Dictionary<Group, Channel<Topic>> ConstructMessageProcessedTriggerChannelMap(Dictionary<Group, GroupTargetsConfiguration> groupTargets)
    {
        return groupTargets.ToDictionary(
            groupTarget => groupTarget.Key,
            _ => Channel.CreateUnbounded<Topic>(
                new UnboundedChannelOptions
                {
                    SingleReader = true,
                    SingleWriter = false
                }));
    }

    private Dictionary<Group, Dictionary<Topic, HandlerChannelContainer>> ConstructMessageHandlersMap(
        IEnumerable<IMessageHandler> messageHandlers,
        Dictionary<Group, GroupTargetsConfiguration> groupTargets)
    {
        return groupTargets.ToDictionary(
            groupTarget => groupTarget.Key,
            groupTarget => GetMessageHandlersForGroupTarget(messageHandlers, groupTarget));
    }

    private Dictionary<Topic, HandlerChannelContainer> GetMessageHandlersForGroupTarget(
        IEnumerable<IMessageHandler> messageHandlers,
        KeyValuePair<Group, GroupTargetsConfiguration> groupTarget)
    {
        return groupTarget.Value.TopicTargets
            .ToDictionary(
                topicTarget => topicTarget.Key,
                topicTarget =>
                {
                    var messageHandler = messageHandlers.SingleOrDefault(x => x.GetType() == topicTarget.Value);
                    if (messageHandler is null)
                    {
                        throw new InvalidOperationException(
                            $"Could not resolve the message handler for topic {topicTarget.Key} and target type {topicTarget.Value}");
                    }

                    return CreateChannelContainerForMessageHandler(messageHandler);
                });
    }

    private HandlerChannelContainer CreateChannelContainerForMessageHandler(IMessageHandler messageHandler)
    {
        return new HandlerChannelContainer(
            messageHandler,
            Channel.CreateBounded<ConsumeResult<byte[], byte[]>>(
                // TODO: move it to constants or make configurable
                new BoundedChannelOptions(100_000)
                {
                    FullMode = BoundedChannelFullMode.Wait,
                    SingleWriter = true,
                    SingleReader = true
                }));
    }

    public async Task<ChannelState> Publish(
        Group group,
        ConsumeResult<byte[], byte[]> message,
        CancellationToken cancellationToken)
    {
        if (!_messageHandlersMap.TryGetValue(group, out var value))
        {
            throw new InvalidOperationException($"Could not resolve message handlers for group {group}");
        }

        if (!value.ContainsKey(message.Topic))
        {
            throw new InvalidOperationException($"Could not resolve the message handler for group {group} and topic {message.Topic}");
        }

        var channel = _messageHandlersMap[group][message.Topic].Channel;
        await channel.Writer.WriteAsync(message, cancellationToken);
        // TODO: move it to constants or make configurable
        return channel.Reader.Count >= 900;
    }

    public async Task PublishAndWait(
        Group group,
        ConsumeResult<byte[], byte[]> message,
        CancellationToken cancellationToken)
    {
        var container = _messageHandlersMap[group][message.Topic];
        await container.Handler.Handle(group, message, cancellationToken);
    }

    public Task SubscribeToMessages(CancellationToken cancellationToken)
    {
        return Task.WhenAll(GetSubscriptionTasks(cancellationToken));
    }

    private IEnumerable<Task> GetSubscriptionTasks(CancellationToken cancellationToken)
    {
        foreach (var (group, topicTargets) in _messageHandlersMap)
        {
            foreach (var (topic, container) in topicTargets)
            {
                yield return ProcessRetrievedMessage(group, topic, container, cancellationToken);
            }
        }
    }

    private async Task ProcessRetrievedMessage(
        Group group,
        Topic topic,
        HandlerChannelContainer container,
        CancellationToken cancellationToken)
    {
        await foreach (var message in container.Channel.Reader.ReadAllAsync(cancellationToken))
        {
            await container.Handler.Handle(group, message, cancellationToken);

            if (ChannelCanBeConsideredFreeing(container.Channel.Reader.Count))
            {
                await PublishMessageProcessedTrigger(group, topic, cancellationToken);
            }
        }
    }

    private bool ChannelCanBeConsideredFreeing(int currentChannelSize)
    {
        // TODO: make configurable.
        return currentChannelSize <= 100;
    }

    private async Task PublishMessageProcessedTrigger(Group group, Topic topic, CancellationToken cancellationToken)
    {
        if (!_messageProcessedTriggerChannelMap.TryGetValue(group, out var value))
        {
            throw new InvalidOperationException($"Could not use event channel, coz it has not been initialized for group {group} yet");
        }

        await value.Writer.WriteAsync(topic, cancellationToken);
    }

    public IAsyncEnumerable<Topic> ReadProcessedMessageTriggers(Group group, CancellationToken cancellationToken)
    {
        if (!_messageProcessedTriggerChannelMap.TryGetValue(group, out var value))
        {
            throw new InvalidOperationException($"Could not use event channel, coz it has not been initialized for group {group} yet");
        }

        return value.Reader.ReadAllAsync(cancellationToken);
    }
    
    // TODO: shutdown and other stuff
}
