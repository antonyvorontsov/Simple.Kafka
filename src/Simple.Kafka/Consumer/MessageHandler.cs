using System;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Simple.Kafka.Common;
using Simple.Kafka.Consumer.Configuration;
using Simple.Kafka.Consumer.Exceptions;
using Simple.Kafka.Consumer.Primitives;

namespace Simple.Kafka.Consumer;

internal sealed class MessageHandler<TConsumer, TKey, TBody> : IMessageHandler<TKey, TBody>
    where TConsumer : IKafkaConsumer<TKey, TBody>
{
    private bool _skipDeserializationErrorsGlobally;
    private readonly string _handlerName;

    private readonly TConsumer _kafkaConsumer;
    private readonly MessageHandlerConfiguration<TKey, TBody> _messageHandlerConfiguration;
    private readonly ICommitStrategyManager _commitStrategyManager;
    private readonly ILogger<MessageHandler<TConsumer, TKey, TBody>> _logger;

    public MessageHandler(
        TConsumer kafkaConsumer,
        IOptions<MessageHandlerConfiguration<TKey, TBody>> messageHandlerConfiguration,
        ICommitStrategyManager commitStrategyManager,
        IOptionsMonitor<GlobalKafkaConsumerConfiguration> globalConfiguration,
        ILogger<MessageHandler<TConsumer, TKey, TBody>> logger)
    {
        _kafkaConsumer = kafkaConsumer;
        _messageHandlerConfiguration = messageHandlerConfiguration.Value;
        _commitStrategyManager = commitStrategyManager;

        _skipDeserializationErrorsGlobally = globalConfiguration.CurrentValue.SkipDeserializationErrorsGlobally;
        globalConfiguration.OnChange(config => _skipDeserializationErrorsGlobally = config.SkipDeserializationErrorsGlobally);
        _logger = logger;

        _handlerName = $"{GetType().Name}<{typeof(TConsumer).Name}, {typeof(TKey).Name}, {typeof(TBody).Name}>";
    }

    public async Task Handle(Group group, ConsumeResult<byte[], byte[]> message, CancellationToken cancellationToken)
    {
        await HandleWithRetries(group, message, cancellationToken);
        await CommitWithRetries(group, message, cancellationToken);
    }

    private async Task HandleWithRetries(
        Group group,
        ConsumeResult<byte[], byte[]> message,
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var skipDeserializationErrors = _skipDeserializationErrorsGlobally
                ? _skipDeserializationErrorsGlobally
                : _messageHandlerConfiguration.SkipDeserializationErrors;

            try
            {
                TKey key;
                try
                {
                    key = _messageHandlerConfiguration.KeyDeserializationFunction(message.Message.Key);
                }
                catch (Exception exception)
                {
                    if (!skipDeserializationErrors) throw;

                    _logger.LogError(exception, "{Prefix} A deserialization error has occurred", Constants.Prefixes.Consumer);
                    return;
                }

                TBody body;
                try
                {
                    body = _messageHandlerConfiguration.BodyDeserializationFunction(message.Message.Value);
                }
                catch (Exception exception)
                {
                    if (!skipDeserializationErrors) throw;

                    _logger.LogError(exception, "{Prefix} A deserialization error has occurred", Constants.Prefixes.Consumer);
                    return;
                }

                var causationId = new CausationId(
                    message.TopicPartitionOffset.Topic,
                    message.TopicPartitionOffset.Partition.Value,
                    message.TopicPartitionOffset.Offset.Value);
                await Handle(
                    group,
                    new KafkaMessage<TKey, TBody>(key, body, message.Message.Headers),
                    causationId,
                    cancellationToken);
                return;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "{Prefix} Could not handle the message from {TopicPartition} due to an exception",
                    Constants.Prefixes.Consumer,
                    message.TopicPartition);
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }
    }

    public Task Handle(
        Group group,
        KafkaMessage<TKey, TBody> message,
        CausationId causationId,
        CancellationToken cancellationToken)
    {
        if (_messageHandlerConfiguration.MessageProcessingTimeout is null)
        {
            return _kafkaConsumer.Handle(message, causationId, cancellationToken);
        }

        using var childCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        childCts.CancelAfter(_messageHandlerConfiguration.MessageProcessingTimeout.Value);
        return _kafkaConsumer.Handle(message, causationId, childCts.Token);
    }

    private async Task CommitWithRetries(
        Group group,
        ConsumeResult<byte[], byte[]> message,
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var strategy = _commitStrategyManager.Get(group);
                if (strategy is null)
                {
                    throw new CommitStrategyException($"Commit strategy for group {group} has not been found");
                }

                strategy(message);

                return;
            }
            catch (KafkaException exception) when (!exception.Error.IsFatal && exception.Error.Code == ErrorCode.Local_State)
            {
                _logger.LogWarning(
                    exception,
                    "{Prefix} Could not commit the message from {TopicPartition} in group {Group} due to a local error",
                    Constants.Prefixes.Consumer,
                    message.TopicPartition,
                    group);
                return;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "{Prefix} Could not commit the message from {TopicPartition} in group {Group} due to an exception",
                    Constants.Prefixes.Consumer,
                    message.TopicPartition,
                    group);
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }
    }
}
