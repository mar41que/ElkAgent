using ElkAgent.Collectors;
using ElkAgent.Configuration;
using ElkAgent.Models;
using ElkAgent.Normalizers;
using ElkAgent.Senders;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ElkAgent.Pipeline;

/// <summary>
/// оркестратор пайплайна: Collect → Normalize → Buffer → Send
/// используе System.Threading.Channels для буферизации
/// </summary>
public class EventPipeline
{
    private readonly IEnumerable<ICollector> _collectors;
    private readonly EcsNormalizer _normalizer;
    private readonly ElasticsearchSender _elasticsearchSender;
    private readonly LogstashSender _logstashSender;
    private readonly AgentConfig _config;
    private readonly ILogger<EventPipeline> _logger;

    private readonly Channel<EcsEvent> _channel = Channel.CreateBounded<EcsEvent>(
        new BoundedChannelOptions(10_000)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false
        });

    public EventPipeline(
        IEnumerable<ICollector> collectors,
        EcsNormalizer normalizer,
        ElasticsearchSender elasticsearchSender,
        LogstashSender logstashSender,
        IOptions<AgentConfig> config,
        ILogger<EventPipeline> logger)
    {
        _collectors = collectors;
        _normalizer = normalizer;
        _elasticsearchSender = elasticsearchSender;
        _logstashSender = logstashSender;
        _config = config.Value;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var collectTasks = _collectors
            .Where(c => c.IsEnabled)
            .Select(collector => CollectAsync(collector, cancellationToken))
            .ToList();

        var sendTask = SendLoopAsync(cancellationToken);

        _logger.LogInformation("Pipeline started with {CollectorCount} collectors, output: {Mode}",
            collectTasks.Count, _config.OutputMode);

        await Task.WhenAll(collectTasks.Append(sendTask));
    }

    private async Task CollectAsync(ICollector collector, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Collector [{Name}] started", collector.Name);
        try
        {
            await foreach (var rawEvent in collector.CollectAsync(cancellationToken))
            {
                var ecsEvent = _normalizer.Normalize(rawEvent);
                await _channel.Writer.WriteAsync(ecsEvent, cancellationToken);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Collector [{Name}] failed", collector.Name);
        }
        finally
        {
            _logger.LogInformation("Collector [{Name}] stopped", collector.Name);
        }
    }

    private async Task SendLoopAsync(CancellationToken cancellationToken)
    {
        var buffer = new List<EcsEvent>(_config.Elasticsearch.BulkSize);
        var flushInterval = TimeSpan.FromMilliseconds(_config.Elasticsearch.FlushIntervalMs);
        var lastFlush = DateTime.UtcNow;

        ISender sender = _config.OutputMode.ToLower() switch
        {
            "logstash" => _logstashSender,
            _ => _elasticsearchSender
        };

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var timeout = flushInterval - (DateTime.UtcNow - lastFlush);
                if (timeout <= TimeSpan.Zero) timeout = TimeSpan.FromMilliseconds(100);

                bool hasItem;
                try
                {
                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    cts.CancelAfter(timeout);
                    var evt = await _channel.Reader.ReadAsync(cts.Token);
                    buffer.Add(evt);
                    hasItem = true;
                }
                catch (OperationCanceledException)
                {
                    hasItem = false;
                }

                bool shouldFlush = buffer.Count >= _config.Elasticsearch.BulkSize
                    || (buffer.Count > 0 && DateTime.UtcNow - lastFlush >= flushInterval);

                if (shouldFlush)
                {
                    await sender.SendBatchAsync(buffer.AsReadOnly(), cancellationToken);
                    _logger.LogDebug("Flushed {Count} events", buffer.Count);
                    buffer.Clear();
                    lastFlush = DateTime.UtcNow;
                }
            }
        }
        catch (OperationCanceledException) { }

        if (buffer.Count > 0)
        {
            _logger.LogInformation("Final flush of {Count} events", buffer.Count);
            await sender.SendBatchAsync(buffer.AsReadOnly(), CancellationToken.None);
        }
    }
}