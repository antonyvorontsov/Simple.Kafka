using System;
using System.Linq;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Simple.Kafka.Consumer;
using Simple.Kafka.Consumer.Builders;
using Simple.Kafka.Consumer.Configuration;
using Simple.Kafka.Consumer.Hosting;

namespace Simple.Kafka.Builders;

public sealed partial class SimpleKafkaBuilder
{
    public SimpleKafkaBuilder AddConsumerGroup(
        string group,
        Action<ConsumerConfig> configure,
        Action<KafkaConsumerGroupConfigurationBuilder> builder)
    {
        if (string.IsNullOrEmpty(group))
        {
            throw new ArgumentException("Group has to be specified", nameof(group));
        }

        TryAddKafkaConsumerInfrastructure();

        var consumerConfig = new ConsumerConfig
        {
            GroupId = group,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            EnableAutoOffsetStore = false
        };
        configure(consumerConfig);

        services.TryAddSingleton<IKafkaBuilderFactory>(new KafkaBuilderFactory(consumerConfig));

        services.Configure<DispatcherConfiguration>(options =>
            options.GroupTargets[group] = new GroupTargetsConfiguration(group));

        var groupConfiguration = new ConsumerGroupConfiguration(group, CommitStrategy.StoreOffset);
        services.Configure<ConsumerGroupsConfiguration>(x => x.GroupConfigurations[group] = groupConfiguration);

        services.AddSingleton(new RegisteredConsumerGroup(group));

        services.AddSingleton<IConsumerGroupManager, ConsumerGroupManager>();

        var builderInstance = new KafkaConsumerGroupConfigurationBuilder(group, services);
        builder?.Invoke(builderInstance);

        return this;
    }

    private void TryAddKafkaConsumerInfrastructure()
    {
        AddHostedServiceOnce<KafkaConsumerStarter>();
        services.TryAddSingleton<IMessageDispatcher, MessageDispatcher>();
        services.TryAddSingleton<ICommitStrategyManager, CommitStrategyManager>();
        return;

        void AddHostedServiceOnce<T>()
            where T : class, IHostedService
        {
            if (services.Any(d =>
                    d.ServiceType == typeof(IHostedService) &&
                    d.ImplementationType == typeof(KafkaConsumerStarter)))
            {
                return;
            }

            services.AddHostedService<T>();
        }
    }
}