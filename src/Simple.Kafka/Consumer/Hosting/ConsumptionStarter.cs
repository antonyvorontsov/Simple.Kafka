using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Simple.Kafka.Consumer.Hosting;

internal sealed class ConsumptionStarter(
    IEnumerable<IConsumerGroupManager> consumerGroupManagers,
    IMessageDispatcher messageDispatcher)
    : BackgroundService
{
    private readonly IReadOnlyCollection<IConsumerGroupManager> _consumerGroupManagers = consumerGroupManagers.ToArray();

    protected override Task ExecuteAsync(CancellationToken cancellationToken) => Start(cancellationToken);

    private Task Start(CancellationToken cancellationToken)
    {
        return Task.WhenAll(GetStarterTasks(cancellationToken).ToArray());
    }

    private IEnumerable<Task> GetStarterTasks(CancellationToken cancellationToken)
    {
        yield return Task.Factory.StartNew(
            () =>
                messageDispatcher.SubscribeToMessages(cancellationToken),
            TaskCreationOptions.LongRunning);

        foreach (var manager in _consumerGroupManagers)
        {
            yield return manager.SubscribeToEvents(cancellationToken);
        }

        foreach (var manager in _consumerGroupManagers)
        {
            yield return Task.Factory.StartNew(
                () => manager.StartGroup(cancellationToken),
                TaskCreationOptions.LongRunning);
        }
    }
}
