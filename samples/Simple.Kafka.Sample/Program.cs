using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Simple.Kafka;
using Simple.Kafka.Common;
using Simple.Kafka.Consumer;
using Simple.Kafka.Consumer.Primitives;
using Simple.Kafka.Producer;
using Simple.Kafka.Sample;

var builder = WebApplication.CreateBuilder(args);

// An example of the simple kafka registration.
builder.Services.AddSimpleKafka(
    // You can also obtain brokers from appsettings, but it has to be done manually on the application side.
    "localhost:9092",
    kafkaBuilder =>
        kafkaBuilder

            // PRODUCERS

            // Registers kafka producer which can send messages to all kafka topics.
            .AddProducer()

            // Registers generic kafka producer that is responsible for sending messages to the concrete kafka topic.
            // Serialization of messages is dependent on C# types.
            // Classes that have an XML attribute will be serialized to XML.
            // Classes that implement the generated protobuf IMessage interface will be serialized to byte arrays.
            // Classes that do not have any of these things will be serialized as JSON.
            // For primitive types no serialization will be done whatsoever, therefore they will be sent as is.
            .AddProducer<CustomKey, CustomBody>("concrete_topic")

            // Registers a kafka enricher which will enrich all the messages with your custom header.
            // You can register as many enrichers as you want.
            .AddProducerHeaderEnricher<ApplicationNameKafkaHeaderEnricher>()

            // You are free to change default producer config as well
            .CustomizeProducerConfiguration(config => config.Partitioner = Partitioner.Murmur2)

            // CONSUMERS

            .AddConsumerGroup("consumer_group_name_foo_bar",
                groupBuilder => groupBuilder.AddConsumer<FirstCustomConsumer, AnotherKey, AnotherBody>(
                        "topic_number_one")
                    .AddConsumer<SecondDifferentConsumer, YetAnotherKey, YetAnotherBody>(
                        "topic_number_two"))
            
            .AddConsumerGroup("another_consumer_group",
                groupBuilder =>
                    groupBuilder.AddConsumer<ConsumerForFirstTopicButDifferentPurposes, AnotherKey, AnotherBody>(
                        "topic_number_one"))
);

builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();
app.UseHttpsRedirection();
app.Run();

namespace Simple.Kafka.Sample
{
    // Producer
    public sealed record CustomKey(string Key);

    public sealed record CustomBody(string Value);

    public sealed class ApplicationNameKafkaHeaderEnricher : IKafkaHeaderEnricher
    {
        public Header? GetHeader()
        {
            return new Header("app", "your_application_name"u8.ToArray());
        }
    }

    // Consumer

    public sealed record AnotherKey(string Key);

    public sealed record AnotherBody(string Value);

    public sealed class FirstCustomConsumer(ILogger<FirstCustomConsumer> logger)
        : IKafkaConsumer<AnotherKey, AnotherBody>
    {
        public Task Handle(
            KafkaMessage<AnotherKey, AnotherBody> message,
            CausationId causationId,
            CancellationToken cancellationToken)
        {
            logger.LogInformation("Consumed something from topic {Topic}", causationId.TopicPartitionOffset.Topic);
            return Task.CompletedTask;
        }
    }

    public sealed record YetAnotherKey(long Key);

    public sealed record YetAnotherBody(decimal Value);

    public sealed class SecondDifferentConsumer
        : IKafkaConsumer<YetAnotherKey, YetAnotherBody>
    {
        public Task Handle(
            KafkaMessage<YetAnotherKey, YetAnotherBody> message,
            CausationId causationId,
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    public sealed class ConsumerForFirstTopicButDifferentPurposes(ILogger<FirstCustomConsumer> logger)
        : IKafkaConsumer<AnotherKey, AnotherBody>
    {
        public Task Handle(
            KafkaMessage<AnotherKey, AnotherBody> message,
            CausationId causationId,
            CancellationToken cancellationToken)
        {
            logger.LogInformation("Doing something else with {Topic} messages", causationId.TopicPartitionOffset.Topic);
            return Task.CompletedTask;
        }
    }
}