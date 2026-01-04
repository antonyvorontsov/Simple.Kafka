# Simple.Kafka

A reasonably simple C# Kafka client for Apache Kafka.

This library allows you to register kafka producers and consumers with no or minimal configuration. Any contributions are welcomed.

## Summary

The library provides an extension method that registers one kafka cluster, producers and consumers within it.

## Producer

The producer functionality allows you to send messages to Kafka topics.

### Setup

To set up the producer, you need to use the `AddSimpleKafka` extension method on `IServiceCollection`. This method takes the Kafka brokers as a string and an action to configure the `SimpleKafkaBuilder`.

```csharp
builder.Services.AddSimpleKafka(
    "localhost:9092",
    kafkaBuilder =>
    {
        // ...
    });
```

Inside the `SimpleKafkaBuilder` configuration, you can use the following methods to set up the producer:

*   `AddKafkaProducer()`: Registers a generic Kafka producer (`IKafkaProducer`) that can send messages to any topic.
*   `AddKafkaProducer<TKey, TBody>(string topic)`: Registers a specific Kafka producer (`IKafkaProducer<TKey, TBody>`) for a given topic. The key and body types are used for serialization.
*   `AddKafkaProducerHeaderEnricher<TEnricher>()`: Registers a header enricher that adds custom headers to **_all_** messages.
*   `SetKafkaProducerConfiguration(Action<ProducerConfig> config)`: Allows you to customize the underlying Confluent Kafka producer configuration.

### Usage

Once the producer is registered, you can inject the following dependencies into your services:

*   `IKafkaProducer`: A generic producer that can be used to send messages to any topic. You need to specify the topic, key, and body for each message.
*   `IKafkaProducer<TKey, TBody>`: A specific producer that is tied to a specific topic. You only need to provide the key and body when sending a message.

Here's an example of how to use the specific producer:

```csharp
public class MyService
{
    private readonly IKafkaProducer<string, string> _producer;

    public MyService(IKafkaProducer<string, string> producer)
    {
        _producer = producer;
    }

    public async Task SendMessage(string key, string message)
    {
        await _producer.ProduceAsync(key, message);
    }
}
```

### Serialization

The library automatically handles serialization based on the type of the key and body:

*   Classes with an `XmlAttribute` will be serialized to XML.
*   Classes that implement the `IMessage` interface from Google.Protobuf will be serialized to Protobuf.
*   Other classes will be serialized to JSON.
*   Primitive types will be sent as is.

## Consumer

Still under development.