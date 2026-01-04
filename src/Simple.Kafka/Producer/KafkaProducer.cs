using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Options;
using Simple.Kafka.Producer.Configuration;
using Simple.Kafka.Producer.Serializers;

namespace Simple.Kafka.Producer;

internal sealed class KafkaProducer(
        IBaseProducer baseProducer,
        IEnumerable<IKafkaProducerHeadersEnricher> enrichers,
        IOptions<KafkaProducerConfiguration> configurationOptions)
    : IKafkaProducer
{
    private readonly KafkaProducerConfiguration _configuration = configurationOptions.Value;

    public async Task Produce<TKey, TBody>(
        string topic,
        TKey key,
        TBody body,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(topic))
        {
            throw new ArgumentException("Topic name is required", nameof(topic));
        }

        var serializationConfiguration = _configuration.GetSerializationConfiguration<TKey, TBody>();
        var keySerializer = serializationConfiguration?.KeySerializer ?? SerializerDetectionExtensions.GetDefaultSerializerOf<TKey>();
        var bodySerializer = serializationConfiguration?.BodySerializer ?? SerializerDetectionExtensions.GetDefaultSerializerOf<TBody>();

        var message = new Message<byte[]?, byte[]?>
        {
            Key = keySerializer.Serialize(key),
            Value = bodySerializer.Serialize(body),
            Headers = ExtractHeaders()
        };
        await baseProducer.Produce(topic, message, cancellationToken);
    }

    public async Task Produce<TKey, TBody>(
        string topic,
        IReadOnlyCollection<KafkaMessage<TKey, TBody>> kafkaMessages,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(topic))
        {
            throw new ArgumentException("Topic name is required", nameof(topic));
        }

        var serializationConfiguration = _configuration.GetSerializationConfiguration<TKey, TBody>();
        var keySerializer = serializationConfiguration?.KeySerializer ?? SerializerDetectionExtensions.GetDefaultSerializerOf<TKey>();
        var bodySerializer = serializationConfiguration?.BodySerializer ?? SerializerDetectionExtensions.GetDefaultSerializerOf<TBody>();

        var headers = ExtractHeaders();
        var messages = kafkaMessages.Select(
                message => new Message<byte[]?, byte[]?>
                {
                    Key = keySerializer.Serialize(message.Key),
                    Value = bodySerializer.Serialize(message.Body),
                    Headers = headers
                })
            .ToArray();
        await baseProducer.Produce(topic, messages, cancellationToken);
    }

    private Headers ExtractHeaders()
    {
        var headers = new Headers();
        foreach (var header in enrichers.Select(x => x.GetHeader())
                     .Where(header => header is not null))
        {
            headers.Add(header);
        }

        return headers;
    }
}

internal sealed class KafkaProducer<TKey, TBody>(
        IBaseProducer baseProducer,
        IEnumerable<IKafkaProducerHeadersEnricher> enrichers,
        IOptions<KafkaProducerConfiguration<TKey, TBody>> producerConfigurationOptions)
    : IKafkaProducer<TKey, TBody>
{
    private readonly KafkaProducerConfiguration<TKey, TBody> _configuration = producerConfigurationOptions.Value;

    public async Task Produce(TKey key, TBody body, CancellationToken cancellationToken)
    {
        var message = new Message<byte[]?, byte[]?>
        {
            Key = _configuration.KeySerializer.Serialize(key),
            Value = _configuration.BodySerializer.Serialize(body),
            Headers = ExtractHeaders()
        };
        await baseProducer.Produce(_configuration.Topic, message, cancellationToken);
    }

    public async Task Produce(IReadOnlyCollection<KafkaMessage<TKey, TBody>> kafkaMessages, CancellationToken cancellationToken)
    {
        var headers = ExtractHeaders();
        var messages = kafkaMessages.Select(
                message => new Message<byte[]?, byte[]?>
                {
                    Key = _configuration.KeySerializer.Serialize(message.Key),
                    Value = _configuration.BodySerializer.Serialize(message.Body),
                    Headers = headers
                })
            .ToArray();
        await baseProducer.Produce(_configuration.Topic, messages, cancellationToken);
    }

    private Headers ExtractHeaders()
    {
        var headers = new Headers();
        foreach (var header in enrichers.Select(x => x.GetHeader())
                     .Where(header => header is not null))
        {
            headers.Add(header);
        }

        return headers;
    }
}
