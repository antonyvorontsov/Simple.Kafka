using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Simple.Kafka.Common;
using Simple.Kafka.Producer.Exceptions;

namespace Simple.Kafka.Producer;

internal sealed class BaseProducer : IBaseProducer
{
    private readonly IProducer<byte[]?, byte[]?> _producer;

    public BaseProducer(ProducerConfig config, ILogger<BaseProducer> logger)
    {
        _producer = new ProducerBuilder<byte[]?, byte[]?>(config)
            .SetLogHandler((_, message) => logger.LogInformation(
                "{Prefix} Kafka producer event has occurred. Level {Level}. Librdkafka client error name {Name}. {Facility}: {Message}",
                Constants.Prefixes.Producer,
                message.Level,
                message.Name,
                message.Facility,
                message.Message))
            .SetErrorHandler((_, error) => logger.LogError(
                "{Prefix} Kafka producer error has occurred. Code {Code}. IsFatal {IsFatal}. Reason: {Reason}",
                Constants.Prefixes.Producer,
                error.Code,
                error.IsFatal,
                error.Reason))
            .Build();
    }

    public async Task Produce(
        string topic,
        Message<byte[]?, byte[]?> message,
        CancellationToken cancellationToken)
    {
        await _producer.ProduceAsync(topic, message, cancellationToken);
    }

    public Task Produce(
        string topic,
        IReadOnlyCollection<Message<byte[]?, byte[]?>> messages,
        CancellationToken cancellationToken)
    {
        if (messages.Count == 0)
        {
            return Task.CompletedTask;
        }

        var completionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var registration = cancellationToken.Register(() => completionSource.TrySetCanceled(cancellationToken));

        var messagesToDeliver = messages.Count;
        var deliveredMessages = 0;

        foreach (var message in messages)
        {
            _producer.Produce(
                topic,
                message,
                deliveryReport =>
                {
                    if (deliveryReport.Error is not null &&
                        deliveryReport.Error.Code is not ErrorCode.NoError)
                    {
                        var exception = new SimpleKafkaProducerException(
                            deliveryReport.Error.Code,
                            deliveryReport.Error.Reason);
                        completionSource.TrySetException(exception);
                    }
                    else
                    {
                        Interlocked.Increment(ref deliveredMessages);
                        if (messagesToDeliver == deliveredMessages)
                        {
                            completionSource.TrySetResult();
                        }
                    }
                });
        }

        return completionSource.Task;
    }
}