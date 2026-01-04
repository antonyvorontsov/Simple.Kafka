using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Simple.Kafka.Consumer.Configuration;
using Simple.Kafka.Consumer.Deserializers;

namespace Simple.Kafka.Consumer.Builders;

public class KafkaConsumerGroupConfigurationBuilder(string group, IServiceCollection services)
{
    public string Group { get; } = group;
    public IServiceCollection Services { get; } = services;
    public List<string> Topics { get; } = new();

    public KafkaConsumerGroupConfigurationBuilder AddConsumer<TConsumer, TKey, TBody>(
        string topic,
        CommitStrategy commitStrategy = CommitStrategy.StoreOffset,
        Action<KafkaConsumerGroupBuilder<TConsumer, TKey, TBody>>? configuration = null)
        where TConsumer : class, IKafkaConsumer<TKey, TBody>
    {
        if (string.IsNullOrEmpty(topic))
        {
            throw new ArgumentException("Topic has to be specified", nameof(topic));
        }

        if (Topics.Contains(topic))
        {
            throw new ArgumentException("Topic has already been registered", nameof(topic));
        }

        Topics.Add(topic);

        Services.Configure<ConsumerGroupsConfiguration>(x =>
            x.GroupConfigurations[Group].AddTopic(topic));

        var consumerBuilder = new KafkaConsumerGroupBuilder<TConsumer, TKey, TBody>();
        configuration?.Invoke(consumerBuilder);

        var keyDeserializer = consumerBuilder.KeyDeserializer ??
                              DeserializerDetectionExtensions.GetDefaultDeserializerOf<TKey>();
        var bodyDeserializer = consumerBuilder.BodyDeserializer ??
                               DeserializerDetectionExtensions.GetDefaultDeserializerOf<TBody>();

        Services.Configure<DispatcherConfiguration>(options =>
            options.GroupTargets[Group].AddTarget(topic, typeof(MessageHandler<TConsumer, TKey, TBody>)));

        Services.Configure<MessageHandlerConfiguration<TKey, TBody>>(options =>
        {
            options.Group = Group;
            options.KeyDeserializationFunction = key => keyDeserializer.Deserialize(key);
            options.BodyDeserializationFunction = body => bodyDeserializer.Deserialize(body);
            options.SkipDeserializationErrors = consumerBuilder.SkipDeserializationErrorsOption;
            options.MessageProcessingTimeout = consumerBuilder.MessageProcessingTimeout;
        });

        Services.AddTransient<TConsumer>();
        Services.AddSingleton<IMessageHandler, MessageHandler<TConsumer, TKey, TBody>>();

        Services.Configure<ConsumerGroupsConfiguration>(x =>
            x.GroupConfigurations[Group].SetCommitStrategy(commitStrategy));

        return this;
    }
}
