# Simple.Kafka

A reasonably simple C# Kafka client for Apache Kafka.

This library allows you to register kafka producers and consumers with no or minimal configuration. Any contributions are welcomed.

## Summary

The library provides an extension method that registers one kafka cluster, producers and consumers within it.

## Full Setup Example

Here's an example of a full setup that includes a producer, a consumer group with two consumers, and custom header enrichment:

```csharp
builder.Services.AddSimpleKafka(
    "localhost:9092",
    kafkaBuilder =>
        kafkaBuilder
            // Producer
            .AddProducer<MyKey, MyBody>("topic-one")
            // Consumer
            .AddConsumerGroup("my-consumer-group", groupBuilder => groupBuilder
                .AddConsumer<MyFirstConsumer, MyKey, MyBody>("topic-one")
                .AddConsumer<MySecondConsumer, AnotherKey, AnotherBody>("topic-two")
                )
            )
);
```

## Producer

The producer functionality allows you to send messages to Kafka topics. The library provides you two types of producers:
- typed producer for specific topic;
- generic producer that can send messages to any topic.

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

*   `AddProducer()`: Registers a generic Kafka producer (`IKafkaProducer`) that can send messages to any topic.
*   `AddProducer<TKey, TBody>(string topic)`: Registers a specific Kafka producer (`IKafkaProducer<TKey, TBody>`) for a given topic. The key and body types are used for serialization.
*   `AddProducerHeaderEnricher<TEnricher>()`: Registers a header enricher that adds custom headers to **_all_** messages.
*   `CustomizeProducerConfiguration(Action<ProducerConfig> config)`: Allows you to customize the underlying Confluent Kafka producer configuration.

### Usage

Once the producer is registered, you can inject the following dependencies into your services:

*   `IKafkaProducer`: A generic producer that can be used to send messages to any topic. You need to specify the topic, key, and body for each message.
*   `IKafkaProducer<TKey, TBody>`: A specific producer that is tied to a specific topic. You only need to provide the key and body when sending a message.

### Serialization

The library automatically handles serialization based on the type of the key and body:

*   Classes with an `XmlAttribute` will be serialized to XML.
*   Classes that implement the `IMessage` interface from Google.Protobuf will be serialized to Protobuf.
*   Other classes will be serialized to JSON.
*   Primitive types will be sent as is.

You can also provide your own custom serializers by using the `SetKeySerializer` and `SetBodySerializer` methods when configuring a producer:

```csharp
kafkaBuilder.AddProducer<MyKey, MyBody>("my-topic", producerBuilder => producerBuilder
    .SetKeySerializer(new MyCustomSerializer())
    .SetBodySerializer(new MyCustomSerializer())
);
```

## Consumer

The consumer functionality allows you to receive and process messages from Kafka topics.

### Setup

To set up consumers, you need to use the `AddSimpleKafka` extension method on `IServiceCollection`, similar to the producer setup.

Inside the `SimpleKafkaBuilder` configuration, you can use the `AddConsumerGroup` method to register a consumer group. This method takes the group name and an action to configure the consumers for that group.

```csharp
kafkaBuilder.AddConsumerGroup("my-consumer-group", groupBuilder => 
{
    // ...
});
```

Within the `AddConsumerGroup` configuration, you can use the `AddConsumer` method to add a consumer for a specific topic.

```csharp
groupBuilder.AddConsumer<MyConsumer, MyKey, MyBody>("my-topic");
```

### Implementing the Consumer

To handle messages, you need to create a class that implements the `IKafkaConsumer<TKey, TBody>` interface. This interface has a single method, `Handle`.

```csharp
public class MyConsumer : IKafkaConsumer<MyKey, MyBody>
{
    public Task Handle(
        KafkaMessage<MyKey, MyBody> message,
        CausationId causationId,
        CancellationToken cancellationToken)
    {
        // Process the message...
        return Task.CompletedTask;
    }
}
```

The `Handle` method receives three parameters:

*   `KafkaMessage<TKey, TBody> message`: The deserialized Kafka message, including the key, body, and headers.
*   `CausationId causationId`: An identifier for tracing and logging, which contains information about the topic, partition, and offset of the message.
*   `CancellationToken cancellationToken`: A cancellation token that is triggered when the application is shutting down.

### Deserialization

The library automatically handles deserialization based on the type of the key and body, similar to the producer's serialization.

You can also provide your own custom deserializers by using the `SetKeyDeserializer` and `SetBodyDeserializer` methods when configuring a consumer:

```csharp
groupBuilder.AddConsumer<MyConsumer, MyKey, MyBody>("my-topic", consumerBuilder => consumerBuilder
    .SetKeyDeserializer(new MyCustomDeserializer())
    .SetBodyDeserializer(new MyCustomDeserializer())
);
```