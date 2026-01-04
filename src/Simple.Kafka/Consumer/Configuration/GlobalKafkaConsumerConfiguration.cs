namespace Simple.Kafka.Consumer.Configuration;

// TODO: Add DI for that
public sealed class GlobalKafkaConsumerConfiguration
{
    public bool SkipDeserializationErrorsGlobally { get; set; }
}
