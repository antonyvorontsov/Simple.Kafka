using Microsoft.Extensions.DependencyInjection;
using Simple.Kafka.Producer.Configuration;
using Simple.Kafka.Producer.Serializers;

namespace Simple.Kafka.Producer.Builders;

public sealed class ProducerSerializationConfigurationBuilder<TKey, TBody>(IServiceCollection services)
{
    public ProducerSerializationConfigurationBuilder<TKey, TBody> SetKeySerializer<TSerializer>(TSerializer serializer)
        where TSerializer : ISerializer
    {
        services.Configure<KafkaProducerConfiguration>(config =>
        {
            var keyBodyPair = (typeof(TKey), typeof(TBody));
            config.Serializers.TryAdd(keyBodyPair, new KafkaProducerSerializationConfiguration());
            config.Serializers[keyBodyPair].KeySerializer = serializer;
        });

        return this;
    }

    public ProducerSerializationConfigurationBuilder<TKey, TBody> SetBodySerializer<TSerializer>(TSerializer serializer)
        where TSerializer : ISerializer
    {
        services.Configure<KafkaProducerConfiguration>(config =>
        {
            var keyBodyPair = (typeof(TKey), typeof(TBody));
            config.Serializers.TryAdd(keyBodyPair, new KafkaProducerSerializationConfiguration());
            config.Serializers[keyBodyPair].BodySerializer = serializer;
        });

        return this;
    }
}