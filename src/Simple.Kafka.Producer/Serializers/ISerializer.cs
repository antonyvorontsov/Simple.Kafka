namespace Simple.Kafka.Producer.Serializers;

public interface ISerializer
{
    public byte[]? Serialize<T>(T value);
}
