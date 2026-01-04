using Microsoft.Extensions.DependencyInjection;

namespace Simple.Kafka.Builders;

public sealed partial class SimpleKafkaBuilder(string brokers, IServiceCollection services);