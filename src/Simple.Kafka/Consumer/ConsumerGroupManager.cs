using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            // TODO: make it safer.
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
                // In that case we do not need to keep trying. That exception occurs only when the consumer is being recreated.
                // That means that other partitions will be assigned to that consumer after its creation.
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
                // TODO: Add Constants.Delays.
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
                // TODO: Add Constants.Delays.
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
                    "{Prefix} Consumption has been stopped due to an error: {Message}. Consumer will be recreated",
                    Constants.Prefixes.Consumer,
                    exception.Message);
                // TODO: Add Constants.Delays.
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }
    }

    private async Task Consume(CancellationToken cancellationToken)
    {
        try
        {
            // Since we control the disposal of the consumer manually, we are sure that these warnings are not a threat.
            // ReSharper disable AccessToDisposedClosure
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
                // TODO: Use (add) millisecondsTimeout configuration for the group.
                return _consumer!.Consume(millisecondsTimeout: 250);
            }
            catch (ConsumeException exception) when (!exception.Error.IsFatal &&
                                                     attempt < 5)
            {
                // TODO: Add Constants.Delays.
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }

        return null;
    }

    private IConsumer<byte[], byte[]> BuildConsumer()
    {
        return new ConsumerBuilder<byte[], byte[]>(_consumerConfig)
            .SetLogHandler(HandleLog)
            .SetErrorHandler(HandleError)
            .SetOffsetsCommittedHandler(HandleOffsetsCommitted)
            .SetPartitionsAssignedHandler(HandleOffsetsAssigned)
            .SetPartitionsRevokedHandler(HandleOffsetsRevoked)
            .Build();


        void HandleLog(IConsumer<byte[], byte[]> _, LogMessage message)
        {
            _logger.LogInformation(
                "{Prefix} Kafka consumer event occured. Level {Level}. Name of librdkafka client {Name}. {Facility}: {Message}",
                Constants.Prefixes.Consumer,
                message.Level,
                message.Name,
                message.Facility,
                message.Message);
        }

        void HandleError(IConsumer<byte[], byte[]> _, Error error)
        {
            if (error.IsFatal)
            {
                _logger.LogError(
                    "{Prefix} Kafka consumer fatal error occured. Code {Code}. Reason: {Reason}",
                    Constants.Prefixes.Consumer,
                    error.Code,
                    error.Reason);
            }
            else
            {
                _logger.LogWarning(
                    "{Prefix} Kafka consumer local error occured. Code {Code}. Reason: {Reason}",
                    Constants.Prefixes.Consumer,
                    error.Code,
                    error.Reason);
            }
        }

        void HandleOffsetsCommitted(IConsumer<byte[], byte[]> _, CommittedOffsets committedOffsets)
        {
            var sb = new StringBuilder();
            foreach (var offset in committedOffsets.Offsets)
            {
                sb.AppendLine(offset.TopicPartitionOffset.ToString());
            }

            if (committedOffsets.Error.IsError)
            {
                _logger.LogError(
                    "{Prefix} Could not commit offsets {Offsets}. Code {Code}. IsFatal {IsFatal}. Reason: {Reason}",
                    Constants.Prefixes.Consumer,
                    sb.ToString(),
                    committedOffsets.Error.Code,
                    committedOffsets.Error.IsFatal,
                    committedOffsets.Error.Reason);
            }
            else
            {
                _logger.LogInformation(
                    "{Prefix} Successfully committed offsets {Offsets}",
                    Constants.Prefixes.Consumer,
                    sb.ToString());
            }
        }

        void HandleOffsetsAssigned(IConsumer<byte[], byte[]> _, List<TopicPartition> topicPartitions)
        {
            foreach (var topicPartition in topicPartitions)
            {
                _logger.LogInformation(
                    "{Prefix} Topic partition {TopicPartition} assigned for group {Group}",
                    Constants.Prefixes.Consumer,
                    topicPartition,
                    _groupConfig.Group);

                if (_pausedPartitions[topicPartition.Topic].Count != 0)
                {
                    _pausedPartitions[topicPartition.Topic].Add(topicPartition);
                }
            }
        }

        void HandleOffsetsRevoked(IConsumer<byte[], byte[]> _, List<TopicPartitionOffset> topicPartitions)
        {
            foreach (var topicPartition in topicPartitions)
            {
                _logger.LogInformation(
                    "{Prefix} Topic partition {TopicPartition} revoked for group {Group}",
                    Constants.Prefixes.Consumer,
                    topicPartition,
                    _groupConfig.Group);

                if (_pausedPartitions[topicPartition.Topic].Count != 0)
                {
                    _pausedPartitions[topicPartition.Topic].Remove(topicPartition.TopicPartition);
                }
            }
        }
    }
}
