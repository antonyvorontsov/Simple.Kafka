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
    private readonly bool _skipDeserializationErrorsGlobally;
    private readonly string _handlerName;

    private readonly TConsumer _kafkaConsumer;
    private readonly MessageHandlerConfiguration<TKey, TBody> _messageHandlerConfiguration;
    private readonly ICommitStrategyManager _commitStrategyManager;
    private readonly ILogger<MessageHandler<TConsumer, TKey, TBody>> _logger;

    public MessageHandler(
        TConsumer kafkaConsumer,
        ICommitStrategyManager commitStrategyManager,
        IOptions<MessageHandlerConfiguration<TKey, TBody>> messageHandlerConfiguration,
        IOptions<GlobalKafkaConsumerConfiguration> globalConfiguration,
        ILogger<MessageHandler<TConsumer, TKey, TBody>> logger)
    {
        _kafkaConsumer = kafkaConsumer;
        _messageHandlerConfiguration = messageHandlerConfiguration.Value;
        _commitStrategyManager = commitStrategyManager;

        _skipDeserializationErrorsGlobally = globalConfiguration.Value.SkipDeserializationErrorsGlobally;
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

                    _logger.LogError(
                        exception, 
                        "{Prefix} {HandlerName} A deserialization error has occurred", 
                        Constants.Prefixes.Consumer,
                        _handlerName);
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

                    _logger.LogError(
                        exception, 
                        "{Prefix} {HandlerName} A deserialization error has occurred", 
                        Constants.Prefixes.Consumer,
                        _handlerName);
                    return;
                }

                await Handle(
                    new KafkaMessage<TKey, TBody>(key, body, message.Message.Headers),
                    new CausationId(message.TopicPartitionOffset),
                    cancellationToken);
                return;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "{Prefix} {HandlerName} Could not handle the message from {TopicPartition} in group {Group} due to an exception",
                    Constants.Prefixes.Consumer,
                    _handlerName,
                    message.TopicPartition,
                    group);
                await Task.Delay(Constants.Delays.DefaultDelay, cancellationToken);
            }
        }
    }

    public Task Handle(
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
                // When we commit a message by a consumer which does not have an assignment to the partition we get an exception.
                // Exception is not as informative and says 'Local: Erroneous state'. Here are some key points:
                //  - the pratition has been reassigned to another consumer;
                //  - since we do not 'own' that partition anymore we cannot commit (or store offset) for that partition;
                //  - messages from that partition are expected to be processed by another consumer;
                //  - we CAN SKIP messages with such exceptions.
                // See the comment - https://github.com/confluentinc/confluent-kafka-dotnet/issues/1861#issuecomment-1207402568
                _logger.LogWarning(
                    exception,
                    "{Prefix} {HandlerName} Could not commit the message from {TopicPartition} in group {Group} due to a local error",
                    Constants.Prefixes.Consumer,
                    _handlerName,
                    message.TopicPartition,
                    group);
                return;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "{Prefix} {HandlerName} Could not commit the message from {TopicPartition} in group {Group} due to an exception",
                    Constants.Prefixes.Consumer,
                    _handlerName,
                    message.TopicPartition,
                    group);
                await Task.Delay(Constants.Delays.DefaultDelay, cancellationToken);
            }
        }
    }
}
