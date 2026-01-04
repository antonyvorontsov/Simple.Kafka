using Confluent.Kafka;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Simple.Kafka;
using Simple.Kafka.Producer;
using Simple.Kafka.Sample;

var builder = WebApplication.CreateBuilder(args);

// An example of the simple kafka registration.
builder.Services.AddSimpleKafka(
    // You can also obtain brokers from appsettings, but it has to be done manually on the application side.
    "localhost:9092",
    kafkaBuilder =>
        kafkaBuilder

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
);

builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();
app.UseHttpsRedirection();
app.Run();

namespace Simple.Kafka.Sample
{
    public sealed record CustomKey(string Key);

    public sealed record CustomBody(string Value);

    public sealed class ApplicationNameKafkaHeaderEnricher : IKafkaHeaderEnricher
    {
        public Header? GetHeader()
        {
            return new Header("app", "your_application_name"u8.ToArray());
        }
    }
}