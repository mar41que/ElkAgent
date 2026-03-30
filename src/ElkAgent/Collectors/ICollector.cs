using ElkAgent.Models;
using System.Threading;

namespace ElkAgent.Collectors;

public interface ICollector
{
    string Name { get; }
    bool IsEnabled { get; }
    IAsyncEnumerable<RawEvent> CollectAsync(CancellationToken cancellationToken);
}