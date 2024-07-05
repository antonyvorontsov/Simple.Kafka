using Simple.Kafka.Producer.Serializers.Defaults;

namespace Simple.Kafka.Producer.Serializers;

internal static class Serializers
{
    internal static class Default
    {
        internal static readonly ISerializer Json = new DefaultJsonSerializer();
        internal static readonly ISerializer Proto = new DefaultProtobufSerializer();
        internal static readonly ISerializer Xml = new DefaultXmlSerializer();
        internal static readonly ISerializer Byte = new DefaultByteSerializer();
    }
}
