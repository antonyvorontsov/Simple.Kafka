using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Simple.Kafka.Common;
using Simple.Kafka.Consumer.Configuration;

namespace Simple.Kafka.Consumer;

internal sealed class ConsumerGroupManager : IConsumerGroupManager
{
    private readonly ConsumerGroupConfiguration _groupConfig;
    private readonly ConsumerConfig _consumerConfig;
    private readonly IMessageDispatcher _messageDispatcher;
    private readonly ICommitStrategyManager _commitStrategyManager;
    private readonly ILogger _logger;

    private IConsumer<byte[], byte[]>? _consumer;

    private readonly Dictionary<Topic, HashSet<TopicPartition>> _pausedPartitions;

    public ConsumerGroupManager(
        ConsumerGroupConfiguration groupConfig,
        ConsumerConfig consumerConfig,
        IMessageDispatcher messageDispatcher,
        ICommitStrategyManager commitStrategyManager,
        ILogger<ConsumerGroupManager> logger)
    {
        _groupConfig = groupConfig;
        _consumerConfig = consumerConfig;
        _messageDispatcher = messageDispatcher;
        _commitStrategyManager = commitStrategyManager;
        _logger = logger;
        _pausedPartitions = groupConfig.Topics.ToDictionary(x => x, _ => new HashSet<TopicPartition>());
    }

    public async Task SubscribeToEvents(CancellationToken cancellationToken)
    {
        await foreach (var messageHandledInTopicEvent in _messageDispatcher.GetMessageProcessedTriggers(
                           _groupConfig.Group,
                           cancellationToken))
        {
            if (_pausedPartitions[messageHandledInTopicEvent].Count == 0)
            {
                continue;
            }

            await ResumePausedTopicPartitions(messageHandledInTopicEvent, cancellationToken);
        }
    }

    private async ValueTask ResumePausedTopicPartitions(Topic topic, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var partitionsToResume = _pausedPartitions[topic];
                await Resume(partitionsToResume, cancellationToken);
                _pausedPartitions[topic].Clear();
                return;
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "{Prefix} Could not resume partitions for topic {Topic} in group {Group}",
                    Constants.Prefixes.Consumer,
                    topic,
                    _groupConfig.Group);
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }
    }

    private async Task Resume(
        IReadOnlyCollection<TopicPartition> partitionsToResume,
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation(
                    "{Prefix} Resuming partitions [{Partitions}]",
                    Constants.Prefixes.Consumer,
                    string.Join(", ", partitionsToResume));
                _consumer!.Resume(partitionsToResume);
                return;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "{Prefix} Could not resume consumption. Error: {Message}",
                    Constants.Prefixes.Consumer,
                    exception.Message);
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }
    }

    public async Task StartGroup(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                using var consumer = BuildConsumer();
                _logger.LogInformation("{Prefix} Consumer has been created", Constants.Prefixes.Consumer);
                
                _consumer = consumer;
                await Consume(cancellationToken);
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "{Prefix} Consumption has been stopped due to an error: {Message}. Consumer will be restarted",
                    Constants.Prefixes.Consumer,
                    exception.Message);
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }
    }

    private async Task Consume(CancellationToken cancellationToken)
    {
        try
        {
            _commitStrategyManager.Set(
                _groupConfig.Group,
                _groupConfig.CommitStrategy switch
                {
                    CommitStrategy.StoreOffset => message => _consumer!.StoreOffset(message),
                    CommitStrategy.Commit => message => _consumer!.Commit(message),
                    _ => throw new InvalidOperationException(
                        $"Commit strategy {_groupConfig.CommitStrategy} cannot be resolved")
                });

            _consumer!.Subscribe(_groupConfig.Topics.Select(x => x.Value));

            while (!cancellationToken.IsCancellationRequested)
            {
                var consumeResult = await ConsumeWithRetries(cancellationToken);
                if (consumeResult is null)
                {
                    continue;
                }

                var channelState = await _messageDispatcher.Publish(
                    _groupConfig.Group,
                    consumeResult,
                    cancellationToken);

                if (channelState.CloseToBeFull)
                {
                    var partitionsToPause = _consumer.Assignment
                        .Where(x => x.Topic == consumeResult.Topic)
                        .ToHashSet();
                    _pausedPartitions[consumeResult.Topic] = partitionsToPause;
                    
                    _logger.LogInformation(
                        "{Prefix} Pausing partitions [{Partitions}]",
                        Constants.Prefixes.Consumer,
                        string.Join(", ", partitionsToPause));
                    _consumer.Pause(partitionsToPause);
                }
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "{Prefix} Could not consume a message properly. Error: {Message}", Constants.Prefixes.Consumer, exception.Message);
        }
        finally
        {
            _logger.LogInformation("{Prefix} Shutting down current consumer", Constants.Prefixes.Consumer);
            foreach (var topicPartitions in _pausedPartitions)
            {
                topicPartitions.Value.Clear();
            }

            _commitStrategyManager.Reset(_groupConfig.Group);
            _consumer?.Unsubscribe();
            _consumer?.Close();
            _consumer?.Dispose();
        }
    }

    private async ValueTask<ConsumeResult<byte[], byte[]>?> ConsumeWithRetries(CancellationToken cancellationToken)
    {
        var attempt = 0;
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                attempt++;
                return _consumer!.Consume(millisecondsTimeout: 250);
            }
            catch (ConsumeException exception) when (!exception.Error.IsFatal &&
                                                     attempt < 5)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }

        return null;
    }

    private IConsumer<byte[], byte[]> BuildConsumer()
    {
        // TODO: Set handler for... methods.
        return new ConsumerBuilder<byte[], byte[]>(_consumerConfig).Build();
    }
}
