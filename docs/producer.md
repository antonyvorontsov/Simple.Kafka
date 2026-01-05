# Producer Configuration

The producer API allows for flexible configuration of Kafka producers. This document details the available configuration options.

## `SimpleKafkaBuilder` Extension Methods

These methods are the entry point for configuring producers and are available within the `AddSimpleKafka` configuration action.

### `AddProducer`

Registers a generic producer (`IKafkaProducer`) that can send messages to any topic.

```csharp
kafkaBuilder.AddProducer(producerBuilder =>
{
    // Configure serialization for specific types
    producerBuilder.For<string, string>()
        .SetKeySerializer(new MyStringSerializer())
        .SetBodySerializer(new MyStringSerializer());
});
```

This method takes an optional action to configure serialization for specific key/body type pairs.

### `AddProducer<TKey, TBody>`

Registers a specific producer (`IKafkaProducer<TKey, TBody>`) for a given topic.

```csharp
kafkaBuilder.AddProducer<MyKey, MyBody>("my-topic", producerBuilder =>
{
    // Configure serialization for this specific producer
    producerBuilder.SetKeySerializer(new MyKeySerializer());
    producerBuilder.SetBodySerializer(new MyBodySerializer());
});
```

This method is useful when you have a producer that always sends messages to the same topic with the same key and body types. It also accepts an action to configure custom serializers for the key and body.

### `AddProducerHeaderEnricher<TEnricher>`

Registers a header enricher that implements `IKafkaHeaderEnricher`. The enricher will be used to add custom headers to **all** messages sent by any producer.

```csharp
kafkaBuilder.AddProducerHeaderEnricher<MyHeaderEnricher>();
```

You can register multiple enrichers.

### `CustomizeProducerConfiguration`

Allows you to customize the underlying Confluent Kafka `ProducerConfig`.

```csharp
kafkaBuilder.CustomizeProducerConfiguration(config =>
{
    config.LingerMs = 100;
});
```

## `ProducerConfigurationBuilder` Methods

This builder is used within the `AddProducer` method.

### `For<TKey, TBody>`

Specifies the key and body types for which to configure serialization. It returns a `ProducerSerializationConfigurationBuilder<TKey, TBody>`.

## `ProducerSerializationConfigurationBuilder<TKey, TBody>` Methods

This builder is returned by `For<TKey, TBody>` and is used to configure serializers for the generic `IKafkaProducer`.

### `SetKeySerializer<TSerializer>`

Sets a custom serializer for the message key.

### `SetBodySerializer<TSerializer>`

Sets a custom serializer for the message body.

## `ProducerConfigurationBuilder<TKey, TBody>` Methods

This builder is used within the `AddProducer<TKey, TBody>` method for specific producers.

### `SetKeySerializer<TSerializer>`

Sets a custom serializer for the message key for the specific producer.

### `SetBodySerializer<TSerializer>`

Sets a custom serializer for the message body for the specific producer.