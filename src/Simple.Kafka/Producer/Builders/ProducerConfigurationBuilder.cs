using Microsoft.Extensions.DependencyInjection;
using Simple.Kafka.Producer.Configuration;
using Simple.Kafka.Producer.Serializers;

namespace Simple.Kafka.Producer.Builders;

// TODO: merge these two together.
public sealed class ProducerConfigurationBuilder(IServiceCollection services)
{
    public ProducerSerializationConfigurationBuilder<TKey, TBody> For<TKey, TBody>()
    {
        return new ProducerSerializationConfigurationBuilder<TKey, TBody>(services);
    }
}

// TODO: merge these two together.
public sealed class ProducerConfigurationBuilder<TKey, TBody>(IServiceCollection services)
{
    public ProducerConfigurationBuilder<TKey, TBody> SetKeySerializer<TSerializer>(TSerializer serializer)
        where TSerializer : ISerializer
    {
        services.Configure<KafkaProducerConfiguration<TKey, TBody>>(
            config => { config.KeySerializer = serializer; });

        return this;
    }

    public ProducerConfigurationBuilder<TKey, TBody> SetBodySerializer<TSerializer>(TSerializer serializer)
        where TSerializer : ISerializer
    {
        services.Configure<KafkaProducerConfiguration<TKey, TBody>>(
            config => { config.BodySerializer = serializer; });

        return this;
    }
}