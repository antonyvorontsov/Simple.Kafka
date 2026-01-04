using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Options;
using Simple.Kafka.Common;
using Simple.Kafka.Producer.Configuration;
using Simple.Kafka.Producer.Serializers;

namespace Simple.Kafka.Producer;

internal sealed class KafkaProducer(
    IBaseProducer baseProducer,
    IEnumerable<IKafkaHeaderEnricher> enrichers,
    IOptions<KafkaProducerConfiguration> configurationOptions)
    : IKafkaProducer
{
    private readonly KafkaProducerConfiguration _configuration = configurationOptions.Value;
    private readonly IKafkaHeaderEnricher[] _enrichers = enrichers.ToArray();

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
        var keySerializer = serializationConfiguration?.KeySerializer ??
                            SerializerDetectionExtensions.GetDefaultSerializerOf<TKey>();
        var bodySerializer = serializationConfiguration?.BodySerializer ??
                             SerializerDetectionExtensions.GetDefaultSerializerOf<TBody>();

        var confluentMessage = new Message<byte[]?, byte[]?>
        {
            Key = keySerializer.Serialize(key),
            Value = bodySerializer.Serialize(body),
            Headers = EnrichHeaders(messageHeaders: null)
        };
        await baseProducer.Produce(topic, confluentMessage, cancellationToken);
    }

    public async Task Produce<TKey, TBody>(
        string topic,
        KafkaMessage<TKey, TBody> message,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(topic))
        {
            throw new ArgumentException("Topic name is required", nameof(topic));
        }

        var serializationConfiguration = _configuration.GetSerializationConfiguration<TKey, TBody>();
        var keySerializer = serializationConfiguration?.KeySerializer ??
                            SerializerDetectionExtensions.GetDefaultSerializerOf<TKey>();
        var bodySerializer = serializationConfiguration?.BodySerializer ??
                             SerializerDetectionExtensions.GetDefaultSerializerOf<TBody>();

        var confluentMessage = new Message<byte[]?, byte[]?>
        {
            Key = keySerializer.Serialize(message.Key),
            Value = bodySerializer.Serialize(message.Body),
            Headers = EnrichHeaders(message.Headers)
        };
        await baseProducer.Produce(topic, confluentMessage, cancellationToken);
    }

    public async Task Produce<TKey, TBody>(
        string topic,
        IReadOnlyCollection<KafkaMessage<TKey, TBody>> messages,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(topic))
        {
            throw new ArgumentException("Topic name is required", nameof(topic));
        }

        var serializationConfiguration = _configuration.GetSerializationConfiguration<TKey, TBody>();
        var keySerializer = serializationConfiguration?.KeySerializer ??
                            SerializerDetectionExtensions.GetDefaultSerializerOf<TKey>();
        var bodySerializer = serializationConfiguration?.BodySerializer ??
                             SerializerDetectionExtensions.GetDefaultSerializerOf<TBody>();

        var confluentMessages = messages.Select(message => new Message<byte[]?, byte[]?>
            {
                Key = keySerializer.Serialize(message.Key),
                Value = bodySerializer.Serialize(message.Body),
                Headers = EnrichHeaders(message.Headers)
            })
            .ToArray();
        await baseProducer.Produce(topic, confluentMessages, cancellationToken);
    }

    private Headers EnrichHeaders(Headers? messageHeaders)
    {
        var carrier = new Dictionary<string, byte[]>();
        foreach (var enricher in _enrichers)
        {
            var header = enricher.GetHeader();
            if (header is null)
            {
                continue;
            }

            carrier[header.Key] = header.GetValueBytes();
        }

        if (messageHeaders is not null)
        {
            foreach (var header in messageHeaders)
            {
                carrier[header.Key] = header.GetValueBytes();
            }
        }

        var headers = new Headers();
        foreach (var (key, bytes) in carrier)
        {
            headers.Add(key, bytes);
        }

        return headers;
    }
}

internal sealed class KafkaProducer<TKey, TBody>(
    IBaseProducer baseProducer,
    IEnumerable<IKafkaHeaderEnricher> enrichers,
    IOptions<KafkaProducerConfiguration<TKey, TBody>> producerConfigurationOptions)
    : IKafkaProducer<TKey, TBody>
{
    private readonly KafkaProducerConfiguration<TKey, TBody> _configuration = producerConfigurationOptions.Value;
    private readonly IKafkaHeaderEnricher[] _enrichers = enrichers.ToArray();

    public async Task Produce(TKey key, TBody body, CancellationToken cancellationToken)
    {
        var confluentMessage = new Message<byte[]?, byte[]?>
        {
            Key = _configuration.KeySerializer.Serialize(key),
            Value = _configuration.BodySerializer.Serialize(body),
            Headers = EnrichHeaders(messageHeaders: null)
        };
        await baseProducer.Produce(_configuration.Topic, confluentMessage, cancellationToken);
    }

    public async Task Produce(
        KafkaMessage<TKey, TBody> message,
        CancellationToken cancellationToken)
    {
        var confluentMessage = new Message<byte[]?, byte[]?>
        {
            Key = _configuration.KeySerializer.Serialize(message.Key),
            Value = _configuration.BodySerializer.Serialize(message.Body),
            Headers = EnrichHeaders(message.Headers)
        };
        await baseProducer.Produce(_configuration.Topic, confluentMessage, cancellationToken);
    }

    public async Task Produce(
        IReadOnlyCollection<KafkaMessage<TKey, TBody>> messages,
        CancellationToken cancellationToken)
    {
        var confluentMessages = messages.Select(message => new Message<byte[]?, byte[]?>
            {
                Key = _configuration.KeySerializer.Serialize(message.Key),
                Value = _configuration.BodySerializer.Serialize(message.Body),
                Headers = EnrichHeaders(message.Headers)
            })
            .ToArray();
        await baseProducer.Produce(_configuration.Topic, confluentMessages, cancellationToken);
    }

    private Headers EnrichHeaders(Headers? messageHeaders)
    {
        var carrier = new Dictionary<string, byte[]>();
        foreach (var enricher in _enrichers)
        {
            var header = enricher.GetHeader();
            if (header is null)
            {
                continue;
            }

            carrier[header.Key] = header.GetValueBytes();
        }

        if (messageHeaders is not null)
        {
            foreach (var header in messageHeaders)
            {
                carrier[header.Key] = header.GetValueBytes();
            }
        }

        var headers = new Headers();
        foreach (var (key, bytes) in carrier)
        {
            headers.Add(key, bytes);
        }

        return headers;
    }
}
