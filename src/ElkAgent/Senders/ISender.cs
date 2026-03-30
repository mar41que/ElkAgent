using ElkAgent.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ElkAgent.Senders;

public interface ISender
{
    Task SendBatchAsync(IReadOnlyList<EcsEvent> events, CancellationToken cancellationToken);
}