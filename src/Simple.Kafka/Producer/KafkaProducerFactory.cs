using Confluent.Kafka;
using Microsoft.Extensions.Logging;

namespace Simple.Kafka.Producer;

internal sealed class KafkaProducerFactory(
        ProducerConfig producerConfig,
        ILogger<KafkaProducerFactory> logger)
    : IKafkaProducerFactory
{
    private readonly ILogger _logger = logger;

    public IProducer<byte[]?, byte[]?> Create()
    {
        return new ProducerBuilder<byte[]?, byte[]?>(producerConfig)
            .SetLogHandler(
                (_, message) => _logger.LogInformation(
                    "Simple.Kafka.Producer. Kafka producer event occured. Level {Level}. Librdkafka client error name {Name}. {Facility}: {Message}",
                    message.Level,
                    message.Name,
                    message.Facility,
                    message.Message))
            .SetErrorHandler(
                (_, error) => _logger.LogError(
                    "Simple.Kafka.Producer. Kafka producer error occured. Code {Code}. IsFatal {IsFatal}. Reason: {Reason}",
                    error.Code,
                    error.IsFatal,
                    error.Reason))
            .Build();
    }
}