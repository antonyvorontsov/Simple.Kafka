using Simple.Kafka.Common;
using Simple.Kafka.Consumer.Primitives;

namespace Simple.Kafka.Consumer.Configuration;

public sealed class DispatcherChannelsConfiguration
{
    public int ChannelCapacity { get; set; } = Constants.Dispatcher.ChannelCapacity;
    public int CapacityToAlmostFullState { get; set; } = Constants.Dispatcher.CapacityToAlmostFullState;
    public Percent FreeCapacityPercentage { get; set; } = new(50);
    public int FreeChannelBoundary => (int)(ChannelCapacity * FreeCapacityPercentage);
}