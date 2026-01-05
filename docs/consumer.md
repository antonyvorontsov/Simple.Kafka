# Consumer Configuration

The consumer API provides a way to configure and register Kafka consumers. This document details the available configuration options.

## `SimpleKafkaBuilder` Extension Methods

These methods are the entry point for configuring consumers and are available within the `AddSimpleKafka` configuration action.

### `AddConsumerGroup`

Registers a new consumer group.

```csharp
kafkaBuilder.AddConsumerGroup(
    "my-consumer-group",
    groupBuilder => 
    {
        // Add consumers to the group
    },
    consumerConfig => 
    {
        // Customize Confluent ConsumerConfig
    },
    CommitStrategy.StoreOffset
);
```

**Parameters:**

*   `group`: The name of the consumer group.
*   `builder`: An action to configure the consumers within the group using `KafkaConsumerGroupConfigurationBuilder`.
*   `configure`: An optional action to customize the underlying Confluent Kafka `ConsumerConfig`.
*   `commitStrategy`: The commit strategy to use for this consumer group. Defaults to `CommitStrategy.StoreOffset`.

### `ConfigureChannels`

Configures the underlying channels used for message dispatching.

```csharp
kafkaBuilder.ConfigureChannels(builder, new DispatcherChannelsConfiguration 
{
    // Configuration
});
```

This is an advanced setting that allows you to control the parallelism and capacity of the message processing channels.

## `KafkaConsumerGroupConfigurationBuilder` Methods

This builder is used within the `AddConsumerGroup` method to add consumers to the group.

### `AddConsumer<TConsumer, TKey, TBody>`

Adds a consumer to the group for a specific topic.

```csharp
groupBuilder.AddConsumer<MyConsumer, MyKey, MyBody>("my-topic", consumerBuilder => 
{
    // Configure this specific consumer
});
```

**Parameters:**

*   `TConsumer`: The type of your consumer class, which must implement `IKafkaConsumer<TKey, TBody>`.
*   `TKey`: The type of the message key.
*   `TBody`: The type of the message body.
*   `topic`: The name of the topic to consume from.
*   `configuration`: An optional action to configure this specific consumer using `KafkaConsumerGroupBuilder<TConsumer, TKey, TBody>`.

## `KafkaConsumerGroupBuilder<TConsumer, TKey, TBody>` Methods

This builder is used within the `AddConsumer` method to configure a specific consumer.

### `SetKeyDeserializer`

Sets a custom deserializer for the message key.

```csharp
consumerBuilder.SetKeyDeserializer(new MyCustomKeyDeserializer());
```

### `SetBodyDeserializer`

Sets a custom deserializer for the message body.

```csharp
consumerBuilder.SetBodyDeserializer(new MyCustomBodyDeserializer());
```

### `SkipDeserializationErrors`

Configures whether to skip messages that fail to deserialize. Defaults to `true`.

```csharp
consumerBuilder.SkipDeserializationErrors(false);
```

### `SetMessageProcessingTimeout`

Sets a timeout for the processing of a single message. If the `Handle` method of your consumer takes longer than this timeout, the processing will be cancelled.

```csharp
consumerBuilder.SetMessageProcessingTimeout(TimeSpan.FromSeconds(30));
```