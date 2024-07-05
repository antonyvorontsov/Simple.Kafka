using Microsoft.Extensions.DependencyInjection;
using Simple.Kafka.Producer.Serializers;

namespace Simple.Kafka.Producer.Configuration.Builders;

public sealed class KafkaProducerConfigurationBuilder(IServiceCollection services)
{
    public KafkaProducerKeyBodyConfigurationBuilder<TKey, TBody> For<TKey, TBody>()
    {
        return new KafkaProducerKeyBodyConfigurationBuilder<TKey, TBody>(services);
    }
}

public sealed class KafkaProducerConfigurationBuilder<TKey, TBody>(IServiceCollection services)
{
    public KafkaProducerConfigurationBuilder<TKey, TBody> SetKeySerializer<TSerializer>(TSerializer serializer)
        where TSerializer : ISerializer
    {
        services.Configure<KafkaProducerConfiguration<TKey, TBody>>(
            config => { config.KeySerializer = serializer; });

        return this;
    }

    public KafkaProducerConfigurationBuilder<TKey, TBody> SetBodySerializer<TSerializer>(TSerializer serializer)
        where TSerializer : ISerializer
    {
        services.Configure<KafkaProducerConfiguration<TKey, TBody>>(
            config => { config.BodySerializer = serializer; });

        return this;
    }
}