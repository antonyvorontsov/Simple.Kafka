using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Simple.Kafka.Common;
using Simple.Kafka.Consumer.Configuration;
using Simple.Kafka.Consumer.Deserializers;
using Simple.Kafka.Consumer.Primitives;

namespace Simple.Kafka.Consumer.Builders;

public class KafkaConsumerGroupConfigurationBuilder(Group group, IServiceCollection services)
{
    internal Group Group { get; } = group;
    internal IServiceCollection Services { get; } = services;
    internal HashSet<Topic> Topics { get; } = new();

    public KafkaConsumerGroupConfigurationBuilder AddConsumer<TConsumer, TKey, TBody>(
        Topic topic,
        Action<KafkaConsumerGroupBuilder<TConsumer, TKey, TBody>>? configuration = null)
        where TConsumer : class, IKafkaConsumer<TKey, TBody>
    {
        if (string.IsNullOrEmpty(topic))
        {
            throw new ArgumentException("Topic has to be specified", nameof(topic));
        }

        if (!Topics.Add(topic))
        {
            throw new ArgumentException("Topic has already been registered", nameof(topic));
        }

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

        return this;
    }
}
