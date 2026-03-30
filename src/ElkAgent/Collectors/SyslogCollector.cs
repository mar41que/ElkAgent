using ElkAgent.Configuration;
using ElkAgent.Models;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ElkAgent.Collectors;

public class SyslogCollector : ICollector
{
    private readonly SyslogCollectorConfig _config;
    private readonly ILogger<SyslogCollector> _logger;

    public string Name => "syslog";
    public bool IsEnabled => _config.Enabled;

    public SyslogCollector(
        IOptions<AgentConfig> options,
        ILogger<SyslogCollector> logger)
    {
        _config = options.Value.Collectors.Syslog;
        _logger = logger;
    }

    public async IAsyncEnumerable<RawEvent> CollectAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var udpClient = new UdpClient(_config.Port);
        _logger.LogInformation("Syslog listener started on UDP port {Port}", _config.Port);

        while (!cancellationToken.IsCancellationRequested)
        {
            UdpReceiveResult result;
            try
            {
                result = await udpClient.ReceiveAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                yield break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Syslog receive error");
                continue;
            }

            var message = Encoding.UTF8.GetString(result.Buffer);

            yield return new RawEvent
            {
                Source = "syslog",
                Timestamp = DateTimeOffset.UtcNow,
                RawMessage = message,
                Fields = new Dictionary<string, object>
                {
                    ["source_ip"] = result.RemoteEndPoint.Address.ToString(),
                    ["source_port"] = (long)result.RemoteEndPoint.Port
                }
            };
        }
    }
}