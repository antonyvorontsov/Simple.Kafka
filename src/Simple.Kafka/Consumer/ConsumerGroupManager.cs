using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Simple.Kafka.Consumer.Configuration;
using Simple.Kafka.Consumer.Primitives;

namespace Simple.Kafka.Consumer;

internal sealed class ConsumerGroupManager : IConsumerGroupManager
{
    private readonly ConsumerGroupConfiguration _groupConfig;
    private readonly ConsumerConfig _consumerConfig;
    private readonly IMessageDispatcher _messageDispatcher;
    private readonly ICommitStrategyManager _commitStrategyManager;
    private readonly IKafkaBuilderFactory _kafkaConsumerFactory;
    private readonly ILogger _logger;

    private IConsumer<byte[], byte[]>? _consumer;

    private readonly Dictionary<Topic, HashSet<TopicPartition>> _pausedPartitions;

    public ConsumerGroupManager(
        ConsumerGroupConfiguration groupConfig,
        ConsumerConfig consumerConfig,
        IMessageDispatcher messageDispatcher,
        ICommitStrategyManager commitStrategyManager,
        IKafkaBuilderFactory kafkaConsumerFactory,
        ILogger<ConsumerGroupManager> logger)
    {
        _groupConfig = groupConfig;
        _consumerConfig = consumerConfig;
        _messageDispatcher = messageDispatcher;
        _commitStrategyManager = commitStrategyManager;
        _kafkaConsumerFactory = kafkaConsumerFactory;
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
                    "Could not resume partitions for topic {Topic} in group {Group}",
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
                    "Resuming partitions [{Partitions}]",
                    string.Join(", ", partitionsToResume));
                _consumer!.Resume(partitionsToResume);
                return;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Could not resume consumption. Error {Message}",
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
                using var consumer = _kafkaConsumerFactory.Create().Build();
                _logger.LogInformation("Consumer has been created");
                
                _consumer = consumer;
                await Consume(cancellationToken);
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Consumption stopped due to the error {Message}. Consumer will be restarted",
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
                        "Pausing partitions [{Partitions}]",
                        string.Join(", ", partitionsToPause));
                    _consumer.Pause(partitionsToPause);
                }
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Could not consume properly in time. Error {Message}", exception.Message);
        }
        finally
        {
            _logger.LogInformation("Shutting down current consumer");
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
}
