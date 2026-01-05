using System;
using System.Collections.Generic;
using System.Linq;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Simple.Kafka.Common;
using Simple.Kafka.Producer;
using Simple.Kafka.Producer.Builders;
using Simple.Kafka.Producer.Configuration;
using Simple.Kafka.Producer.Serializers;

namespace Simple.Kafka.Builders;

public sealed partial class SimpleKafkaBuilder
{
    private readonly ProducerConfig _defaultProducerConfig = new()
    {
        Acks = Acks.All,
        QueueBufferingMaxMessages = 1_000_000,
        QueueBufferingMaxKbytes = 2_097_152,
        BatchSize = 4_194_304,
        LingerMs = 50,
        BatchNumMessages = 10_000,
        BootstrapServers = brokers
    };
    
    public SimpleKafkaBuilder AddProducer(
        Action<ProducerConfigurationBuilder>? builder = null)
    {
        RegisterBaseProducer(_defaultProducerConfig);
        services.TryAddSingleton<IKafkaProducer, KafkaProducer>();
        services.Configure<KafkaProducerConfiguration>(config =>
        {
            config.Serializers =
                new Dictionary<KafkaProducerKeyBodyTypePair, KafkaProducerSerializationConfiguration>();
        });

        var builderInstance = new ProducerConfigurationBuilder(services);
        builder?.Invoke(builderInstance);

        return this;
    }

    public SimpleKafkaBuilder AddProducer<TKey, TBody>(
        Topic topic,
        Action<ProducerConfigurationBuilder<TKey, TBody>>? builder = null)
    {
        if (string.IsNullOrEmpty(topic))
        {
            throw new ArgumentException("Topic name is required", nameof(topic));
        }

        RegisterBaseProducer(_defaultProducerConfig);
        services.Configure<KafkaProducerConfiguration<TKey, TBody>>(config =>
        {
            config.Topic = topic;
            config.KeySerializer = SerializerDetectionExtensions.GetDefaultSerializerOf<TKey>();
            config.BodySerializer = SerializerDetectionExtensions.GetDefaultSerializerOf<TBody>();
        });
        services.TryAddSingleton<IKafkaProducer<TKey, TBody>, KafkaProducer<TKey, TBody>>();

        var builderInstance = new ProducerConfigurationBuilder<TKey, TBody>(services);
        builder?.Invoke(builderInstance);

        return this;
    }

    public SimpleKafkaBuilder AddProducerHeaderEnricher<TEnricher>()
        where TEnricher : class, IKafkaHeaderEnricher
    {
        services.AddTransient<IKafkaHeaderEnricher, TEnricher>();

        return this;
    }

    public SimpleKafkaBuilder CustomizeProducerConfiguration(
        Action<ProducerConfig>? configuration = null)
    {
        var producerConfig = new ProducerConfig();
        configuration?.Invoke(producerConfig);

        // Setting bootstrap servers right after the action invocation
        // because this library is intended to work with only one kafka cluster.
        // Thus, we do not allow any overrides after the cluster registration is done.
        producerConfig.BootstrapServers = brokers;

        RemoveBaseProducer();
        RegisterBaseProducer(producerConfig);

        return this;
    }

    private void RemoveBaseProducer()
    {
        var serviceDescriptor = services.FirstOrDefault(descriptor => descriptor.ServiceType == typeof(IBaseProducer));
        if (serviceDescriptor is not null)
        {
            services.Remove(serviceDescriptor);
        }
    }

    private void RegisterBaseProducer(ProducerConfig producerConfig)
    {
        services.AddSingleton<IBaseProducer>(provider => new BaseProducer(
            producerConfig,
            provider.GetRequiredService<ILogger<BaseProducer>>()));
    }
}