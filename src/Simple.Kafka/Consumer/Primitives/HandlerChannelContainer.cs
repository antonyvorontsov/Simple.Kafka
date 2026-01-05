using System.Threading.Channels;
using Confluent.Kafka;

namespace Simple.Kafka.Consumer.Primitives;

internal sealed record HandlerChannelContainer(
    IMessageHandler Handler,
    Channel<ConsumeResult<byte[], byte[]>> Channel);
