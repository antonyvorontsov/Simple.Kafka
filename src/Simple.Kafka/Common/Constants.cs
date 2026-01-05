namespace Simple.Kafka.Common;

internal static class Constants
{
    internal static class Prefixes
    {
        internal const string Consumer = "Simple.Kafka.Consumer.";
        internal const string Producer = "Simple.Kafka.Producer.";
    }

    internal static class Dispatcher
    {
        internal const int ChannelCapacity = 100_000;
        private const int ChannelFullnessBufferSize = 5_000;
        internal const int CapacityToAlmostFullState = ChannelCapacity - ChannelFullnessBufferSize;
    }
    
    internal static class Delays
    {
        internal const int DefaultDelay = 100;
    }
}
