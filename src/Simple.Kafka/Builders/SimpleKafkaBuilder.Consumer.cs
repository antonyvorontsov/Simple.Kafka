using System;
using System.Linq;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Simple.Kafka.Consumer;
using Simple.Kafka.Consumer.Builders;
using Simple.Kafka.Consumer.Configuration;
using Simple.Kafka.Consumer.Hosting;
using Simple.Kafka.Consumer.Primitives;

namespace Simple.Kafka.Builders;

public sealed partial class SimpleKafkaBuilder
{
    public SimpleKafkaBuilder AddConsumerGroup(
        Group group,
        Action<KafkaConsumerGroupConfigurationBuilder> builder,
        Action<ConsumerConfig>? configure = null,
        CommitStrategy commitStrategy = CommitStrategy.StoreOffset)
    {
        if (string.IsNullOrEmpty(group))
        {
            throw new ArgumentException("Group has to be specified", nameof(group));
        }

        TryAddKafkaConsumerInfrastructure();

        services.Configure<DispatcherConfiguration>(options =>
            options.GroupTargets[group] = new GroupTargetsConfiguration(group));

        var groupConfiguration = new ConsumerGroupConfiguration(group, commitStrategy);
        services.Configure<ConsumerGroupsConfiguration>(x => x.GroupConfigurations[group] = groupConfiguration);

        services.AddSingleton(new RegisteredConsumerGroup(group));

        services.AddSingleton<IConsumerGroupManager>(
            provider =>
            {
                var groupManagerConfiguration = provider.GetService<IOptions<ConsumerGroupsConfiguration>>()!
                    .Value.GroupConfigurations[group];

                var consumerConfig = new ConsumerConfig
                {
                    GroupId = group,
                    AutoOffsetReset = AutoOffsetReset.Earliest,
                    EnableAutoCommit = false,
                    EnableAutoOffsetStore = false
                };
                configure?.Invoke(consumerConfig);

                return new ConsumerGroupManager(
                    groupManagerConfiguration,
                    consumerConfig,
                    provider.GetRequiredService<IMessageDispatcher>(),
                    provider.GetRequiredService<ICommitStrategyManager>(),
                    provider.GetRequiredService<ILogger<ConsumerGroupManager>>());
            });

        services.Configure<ConsumerGroupsConfiguration>(x =>
            x.GroupConfigurations[group].SetCommitStrategy(commitStrategy));

        var builderInstance = new KafkaConsumerGroupConfigurationBuilder(group, services);
        builder.Invoke(builderInstance);

        return this;
    }

    private void TryAddKafkaConsumerInfrastructure()
    {
        AddHostedServiceOnce<ConsumptionStarter>();
        services.TryAddSingleton<IMessageDispatcher, MessageDispatcher>();
        services.TryAddSingleton<ICommitStrategyManager, CommitStrategyManager>();
        return;

        void AddHostedServiceOnce<T>()
            where T : class, IHostedService
        {
            if (services.Any(d =>
                    d.ServiceType == typeof(IHostedService) &&
                    d.ImplementationType == typeof(ConsumptionStarter)))
            {
                return;
            }

            services.AddHostedService<T>();
        }
    }
}