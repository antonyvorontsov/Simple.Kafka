using System;
using System.Collections.Generic;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Simple.Kafka.Producer.Configuration;
using Simple.Kafka.Producer.Configuration.Builders;
using Simple.Kafka.Producer.Serializers;

namespace Simple.Kafka.Producer;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddKafkaProducer(
        this IServiceCollection services,
        Action<KafkaProducerConfigurationBuilder>? builder = null)
    {
        services.TryAddKafkaProducerInfrastructure();
        services.TryAddSingleton<IKafkaProducer, KafkaProducer>();
        services.Configure<KafkaProducerConfiguration>(
            config =>
            {
                config.Serializers =
                    new Dictionary<KafkaProducerKeyBodyTypePair, KafkaProducerSerializationConfiguration>();
            });

        var builderInstance = new KafkaProducerConfigurationBuilder(services);
        builder?.Invoke(builderInstance);

        return services;
    }

    public static IServiceCollection AddKafkaProducer<TKey, TBody>(
        this IServiceCollection services,
        string topic,
        Action<KafkaProducerConfigurationBuilder<TKey, TBody>>? builder = null)
    {
        if (string.IsNullOrEmpty(topic))
        {
            throw new ArgumentException("Topic name is required", nameof(topic));
        }

        services.TryAddKafkaProducerInfrastructure();
        services.Configure<KafkaProducerConfiguration<TKey, TBody>>(
            config =>
            {
                config.Topic = topic;
                config.KeySerializer = SerializerDetectionExtensions.GetDefaultSerializerOf<TKey>();
                config.BodySerializer = SerializerDetectionExtensions.GetDefaultSerializerOf<TBody>();
            });
        services.TryAddSingleton<IKafkaProducer<TKey, TBody>, KafkaProducer<TKey, TBody>>();

        var builderInstance = new KafkaProducerConfigurationBuilder<TKey, TBody>(services);
        builder?.Invoke(builderInstance);

        return services;
    }

    public static IServiceCollection AddKafkaProducerHeaderEnricher<TEnricher>(this IServiceCollection services)
        where TEnricher : class, IKafkaHeaderEnricher
    {
        services.AddTransient<IKafkaHeaderEnricher, TEnricher>();

        return services;
    }

    public static IServiceCollection SetKafkaProducerConfiguration(
        this IServiceCollection services,
        Action<ProducerConfig>? configuration = null)
    {
        var producerConfig = new ProducerConfig
        {
            Acks = Acks.All,
            QueueBufferingMaxMessages = 1_000_000,
            QueueBufferingMaxKbytes = 2_097_152,
            BatchSize = 4_194_304,
            LingerMs = 50,
            BatchNumMessages = 10_000
        };
        configuration?.Invoke(producerConfig);

        services.TryAddSingleton(
            provider =>
            {
                var brokers = provider.GetRequiredService<IOptions<KafkaConnectionConfiguration>>().Value.Brokers;
                if (string.IsNullOrEmpty(brokers))
                {
                    throw new InvalidOperationException(
                        $"Brokers are not configured. Use AddKafka beforehand");
                }

                producerConfig.BootstrapServers = brokers;
                return producerConfig;
            });

        return services;
    }

    private static IServiceCollection TryAddKafkaProducerInfrastructure(
        this IServiceCollection services)
    {
        services.SetKafkaProducerConfiguration();

        services.TryAddSingleton<KafkaProducerFactory>();
        services.TryAddSingleton<IBaseProducer, BaseProducer>();

        return services;
    }
}