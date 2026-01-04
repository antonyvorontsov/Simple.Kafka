using System;
using Microsoft.Extensions.DependencyInjection;
using Simple.Kafka.Builders;

namespace Simple.Kafka;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSimpleKafka(
        this IServiceCollection services,
        string brokers, 
        Action<SimpleKafkaBuilder> builder)
    {
        if (string.IsNullOrEmpty(brokers))
        {
            throw new ArgumentException("Brokers connection string is required", nameof(brokers));
        }

        builder.Invoke(new SimpleKafkaBuilder(brokers, services));
        return services;
    }
}